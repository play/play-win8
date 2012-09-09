using System;
using System.Reactive.Concurrency;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
