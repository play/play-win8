using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reactive.Windows.Foundation;
using System.Text;
using System.Threading.Tasks;
using Ninject;
using ReactiveUI;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace Play.Models
{
    public interface IPlayApi
    {
        IObservable<Song> NowPlaying();
        IObservable<BitmapImage> FetchImageForAlbum(Song song);
        IObservable<string> ListenUrl();
        IObservable<List<Song>> Queue();
        IObservable<Unit> QueueSong(Song song);
        IObservable<Unit> Star(Song song);
        IObservable<Unit> Unstar(Song song);
        IObservable<List<Song>> Search(string query);
        IObservable<List<Song>> AllSongsForArtist(string name);
        IObservable<List<Song>> AllSongsOnAlbum(string artist, string album);

        IObservable<Unit> ConnectToSongChangeNotifications();
    }

    public class StreamingInfo
    {
        public string stream_url { get; set; }
        public string pusher_key { get; set; }
    }

    public class PlayApi : IPlayApi, IEnableLogger
    {
        readonly HttpClient client;
        readonly Func<HttpMethod, string, HttpRequestMessage> rqFactory;

        [Inject]
        public PlayApi(HttpClient authedClient, Func<HttpMethod, string, HttpRequestMessage> requestFactory)
        {
            client = authedClient;
            rqFactory = requestFactory;
        }

        public IObservable<Song> NowPlaying()
        {
            var rq = rqFactory(HttpMethod.Get, "now_playing");
            return client.RequestAsync<Song>(rq);
        }

        public IObservable<List<Song>> Queue()
        {
            var rq = rqFactory(HttpMethod.Get, "queue");
            return client.RequestAsync<SongQueue>(rq)
                .Select(x => x.songs.ToList());
        }

        public IObservable<Unit> QueueSong(Song song)
        {
            var rq = rqFactory(HttpMethod.Post, WebUtility.UrlEncode(String.Format("queue?id={0}", song.id)));
            return client.RequestAsync(rq).Select(_ => Unit.Default);
        }

        public IObservable<Unit> Star(Song song)
        {
            var rq = rqFactory(HttpMethod.Post, WebUtility.UrlEncode(String.Format("star?id={0}", song.id)));
            return client.RequestAsync(rq).Select(_ => Unit.Default);
        }

        public IObservable<Unit> Unstar(Song song)
        {
            var rq = rqFactory(HttpMethod.Delete, WebUtility.UrlEncode(String.Format("star?id={0}", song.id)));
            return client.RequestAsync(rq).Select(_ => Unit.Default);
        }

        public IObservable<BitmapImage> FetchImageForAlbum(Song song)
        {
            var rq = rqFactory(HttpMethod.Get, String.Format("images/art/{0}.png", song.id));
            return client.RequestAsync(rq)
                .SelectMany(x => x.Content.ReadAsByteArrayAsync())
                .ObserveOn(RxApp.DeferredScheduler)
                .SelectMany(x => {
                        var ret = new BitmapImage();
                        var mem = new MemoryRandomAccessStream(x);
                        return ret.SetSourceAsync(mem).ToObservable().Select(_ => ret);
                });
        }

        public IObservable<List<Song>> Search(string query)
        {
            var rq = rqFactory(HttpMethod.Get, WebUtility.UrlEncode("search?q=" + query));
            return client.RequestAsync<SongQueue>(rq).Select(x => x.songs);
        }

        public IObservable<List<Song>> AllSongsForArtist(string name)
        {
            // NB: https://github.com/play/play/issues/135
            var rq = rqFactory(HttpMethod.Get, String.Format("artist/{0}", WebUtility.UrlEncode(name).Replace("+", "%20")));
            return client.RequestAsync<SongQueue>(rq).Select(x => x.songs);
        }

        public IObservable<List<Song>> AllSongsOnAlbum(string artist, string album)
        {
            // NB: https://github.com/play/play/issues/135
            var rq = rqFactory(HttpMethod.Get, String.Format("artist/{0}/album/{1}", 
                WebUtility.UrlEncode(artist).Replace("+", "%20"), 
                WebUtility.UrlEncode(album).Replace("+", "%20")));

            return client.RequestAsync<SongQueue>(rq).Select(x => x.songs);
        }

        public IObservable<Unit> ConnectToSongChangeNotifications()
        {
            // TODO: Port Pusher to Fx45 WebSockets
            return Observable.Never<Unit>();
            /*
            var rq = new RestRequest("streaming_info");

            return client.RequestAsync<StreamingInfo>(rq)
                .SelectMany(x => PusherHelper.Connect<object>(() => new Pusher(x.Data.pusher_key), "now_playing_updates", "update_now_playing"))
                .Select(_ => Unit.Default);
            */
        }

        public IObservable<string> ListenUrl()
        {
            var rq = rqFactory(HttpMethod.Get, "streaming_info");
            return client.RequestAsync<StreamingInfo>(rq).Select(x => x.stream_url);
        }
    }

    class MemoryRandomAccessStream : IRandomAccessStream
    {
        Stream internalStream;

        public MemoryRandomAccessStream(Stream stream)
        {
            this.internalStream = stream;
        }

        public MemoryRandomAccessStream(byte[] bytes)
        {
            this.internalStream = new MemoryStream(bytes);
        }

        public IInputStream GetInputStreamAt(ulong position)
        {
            this.internalStream.Seek((long)position, SeekOrigin.Begin);

            return this.internalStream.AsInputStream();
        }

        public IOutputStream GetOutputStreamAt(ulong position)
        {
            this.internalStream.Seek((long)position, SeekOrigin.Begin);

            return this.internalStream.AsOutputStream();
        }

        public ulong Size
        {
            get { return (ulong)this.internalStream.Length; }
            set { this.internalStream.SetLength((long)value); }
        }

        public bool CanRead
        {
            get { return true; }
        }

        public bool CanWrite
        {
            get { return true; }
        }

        public IRandomAccessStream CloneStream()
        {
            throw new NotSupportedException();
        }

        public ulong Position
        {
            get { return (ulong)this.internalStream.Position; }
        }

        public void Seek(ulong position)
        {
            this.internalStream.Seek((long)position, 0);
        }

        public void Dispose()
        {
            this.internalStream.Dispose();
        }

        public Windows.Foundation.IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            var inputStream = this.GetInputStreamAt(0);
            return inputStream.ReadAsync(buffer, count, options);
        }

        public Windows.Foundation.IAsyncOperation<bool> FlushAsync()
        {
            var outputStream = this.GetOutputStreamAt(0);
            return outputStream.FlushAsync();
        }

        public Windows.Foundation.IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            var outputStream = this.GetOutputStreamAt(0);
            return outputStream.WriteAsync(buffer);
        }
    }
}
