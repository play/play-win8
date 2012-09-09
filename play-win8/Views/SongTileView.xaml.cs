using System;
using System.Reactive.Concurrency;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Play.ViewModels;
using ReactiveUI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Play.Views
{
    public sealed partial class SongTileView : UserControl, IViewFor<SongTileViewModel>
    {
        public SongTileView()
        {
            this.InitializeComponent();

            RxApp.DeferredScheduler.Schedule(() => {
                this.OneWayBind(ViewModel, x => x.AlbumArt);
                this.OneWayBind(ViewModel, x => x.Model.name);
            });
        }

        public SongTileViewModel ViewModel {
            get { return (SongTileViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(SongTileViewModel), typeof(SongTileView), new PropertyMetadata(null));

        object IViewFor.ViewModel {
            get { return ViewModel; }
            set { ViewModel = (SongTileViewModel)value; }
        }
    }
}
