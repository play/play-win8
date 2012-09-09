using System;
using Play.Models;
using ReactiveUI;

namespace Play.ViewModels
{
    public interface ILoginMethods : IReactiveNotifyPropertyChanged
    {
        IPlayApi CurrentAuthenticatedClient { get; set; }

        void EraseCredentialsAndNavigateToLogin();
        void SaveCredentials(string baseUrl, string username);
        IObservable<IPlayApi> LoadCredentials();
    }
}