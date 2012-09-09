using System;
using System.Reactive.Concurrency;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Play.Common;
using Play.ViewModels;
using ReactiveUI;
using ReactiveUI.Routing;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Play.Views
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class PlayView : Play.Common.LayoutAwarePage, IViewFor<PlayViewModel>
    {
        public PlayView()
        {
            this.InitializeComponent();

            RxApp.DeferredScheduler.Schedule(() => {
                this.OneWayBind(ViewModel, x => x.AllSongs);
                this.WhenAny(x => x.ViewModel.CurrentSong.name, x => x.Value ?? "")
                    .BindTo(this, x => x.name.Text);
                this.WhenAny(x => x.ViewModel.CurrentSong.album, x => x.Value ?? "")
                    .BindTo(this, x => x.album.Text);

                this.WhenAny(x => x.ViewModel.AllSongs, x => x.Value)
                    .Where(x => x != null && x.Any())
                    .SelectMany(x => x.First().WhenAny(y => y.AlbumArt, y => y.Value))
                    .BindTo(this, x => x.CurrentAlbumArt.Source);
            });

            this.PointerReleased += (o, e) =>
                {
                    var v = this;
                };
        }

        public PlayViewModel ViewModel {
            get { return (PlayViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(IPlayViewModel), typeof(PlayView), new PropertyMetadata(null));

        object IViewFor.ViewModel {
            get { return ViewModel; }
            set { ViewModel = (PlayViewModel) value; }
        }
    }
}
