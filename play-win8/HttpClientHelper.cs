using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Net.Http;
using System.Reactive.Threading.Tasks;
using Newtonsoft.Json;

namespace Play
{
    public static class HttpClientRxMixins
    {
        public static IObservable<T> RequestAsync<T>(this HttpClient This, string requestUri)
        {
            return This.GetAsync(requestUri).ToObservable()
                .ThrowOnRestResponseFailure()
                .SelectMany(x => x.Content.ReadAsStringAsync().ToObservable())
                .SelectMany(x => JsonConvert.DeserializeObjectAsync<T>(x).ToObservable());
        }

        public static IObservable<T> RequestAsync<T>(this HttpClient This, HttpRequestMessage request)
        {
            return This.SendAsync(request).ToObservable()
                .ThrowOnRestResponseFailure()
                .SelectMany(x => x.Content.ReadAsStringAsync().ToObservable())
                .SelectMany(x => JsonConvert.DeserializeObjectAsync<T>(x).ToObservable());
        }

        public static IObservable<HttpResponseMessage> RequestAsync(this HttpClient This, HttpRequestMessage request)
        {
            return This.SendAsync(request).ToObservable().ThrowOnRestResponseFailure();
        }

        public static IObservable<HttpResponseMessage> ThrowOnRestResponseFailure(this IObservable<HttpResponseMessage> This)
        {
            return This.SelectMany(x => {
                try {
                    x.EnsureSuccessStatusCode();
                } catch (Exception ex) {
                    return Observable.Throw<HttpResponseMessage>(ex);
                }
                                            
                return Observable.Return(x);
            });
        }
    }
}