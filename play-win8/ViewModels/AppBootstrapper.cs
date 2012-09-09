using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using Ninject;
using Play.Models;
using Play.Views;
using ReactiveUI;
using ReactiveUI.Routing;
using Windows.Security.Credentials;
using Windows.UI.Core;

namespace Play.ViewModels
{
    public interface IAppBootstrapper : IScreen, ILoginMethods { }

    public class AppBootstrapper : ReactiveObject, IAppBootstrapper
    {
        public IRoutingState Router { get; protected set; }

        readonly Func<IObservable<IPlayApi>> apiFactory;
        public AppBootstrapper(IKernel testKernel = null, IRoutingState router = null)
        {
            Kernel = testKernel ?? createDefaultKernel();
            Kernel.Bind<IAppBootstrapper>().ToConstant(this);
            Router = router ?? new RoutingState();

            // XXX: This is a ReactiveUI Bug
            RxApp.DeferredScheduler = CoreDispatcherScheduler.Current;

            apiFactory = Kernel.TryGet<Func<IObservable<IPlayApi>>>("ApiFactory");

            RxApp.ConfigureServiceLocator(
                (type, contract) => Kernel.Get(type, contract),
                (type, contract) => Kernel.GetAll(type, contract),
                (c, i, s) => Kernel.Bind(i).To(c));

            LoadCredentials().Subscribe(
                x => {
                    CurrentAuthenticatedClient = x;
                    Router.Navigate.Execute(Kernel.Get<IPlayViewModel>());
                }, ex => {
                    this.Log().WarnException("Failed to load credentials, going to login screen", ex);
                    Router.Navigate.Execute(Kernel.Get<IPlayViewModel>());
                });
        }

        public static IKernel Kernel { get; protected set; }

        IPlayApi _CurrentAuthenticatedClient;
        public IPlayApi CurrentAuthenticatedClient {
            get { return _CurrentAuthenticatedClient; }
            set { this.RaiseAndSetIfChanged(x => x.CurrentAuthenticatedClient, value); }
        }
        
        /*
         * ILoginMethods
         */

        public void EraseCredentialsAndNavigateToLogin()
        {
            var vault = new PasswordVault();
            foreach(var v in vault.FindAllByResource("play")) { vault.Remove(v); }

            Router.Navigate.Execute(Kernel.Get<IWelcomeViewModel>());
        }

        public void SaveCredentials(string baseUrl, string username)
        {
            var vault = new PasswordVault();
            vault.Add(new PasswordCredential() { Resource = "play", UserName = "BaseUrl", Password = baseUrl });
            vault.Add(new PasswordCredential() { Resource = "play", UserName = "Token", Password = username });

            CurrentAuthenticatedClient = createPlayApiFromCreds(baseUrl, username);
        }

        public IObservable<IPlayApi> LoadCredentials() { return apiFactory != null ? apiFactory() : loadCredentials().ToObservable(); }
        Task<IPlayApi> loadCredentials()
        {
            var vault = new PasswordVault();

            try {
                var baseUrl = vault.FindAllByUserName("BaseUrl").First().Password;
                var token = vault.FindAllByUserName("Token").First().Password;
                return Observable.Return((IPlayApi)createPlayApiFromCreds(baseUrl, token)).ToTask();
            } catch (Exception ex) {
                return Observable.Throw<IPlayApi>(ex).ToTask();
            }
        }

        PlayApi createPlayApiFromCreds(string baseUrl, string token)
        {
            var rc = new HttpClient() {BaseAddress = new Uri(baseUrl)};
            var ret = new PlayApi(rc, (m,p) => {
                var rq = new HttpRequestMessage(m, p);
                rq.Headers.Add("Authorization", token);
                return rq;
            });

            return ret;
        }

        IKernel createDefaultKernel()
        {
            var ret = new StandardKernel();

            ret.Bind<IScreen>().ToConstant(this);
            ret.Bind<ILoginMethods>().ToConstant(this);
            ret.Bind<IWelcomeViewModel>().To<WelcomeViewModel>();
            ret.Bind<IPlayViewModel>().To<PlayViewModel>();
            ret.Bind<ISearchViewModel>().To<SearchViewModel>();

            ret.Bind<IViewFor<PlayViewModel>>().To<PlayView>();

            return ret;
        }
    }
}