using MiHoYoTools.Core;
using Microsoft.UI.Xaml.Controls;

namespace MiHoYoTools.Views
{
    public sealed partial class GachaHostView : Page
    {
        public GachaHostView()
        {
            InitializeComponent();
            LoadGameView(GameContext.Current.CurrentGame);
            GameContext.Current.GameChanged += OnGameChanged;
        }

        private void OnGameChanged(object sender, GameType game)
        {
            LoadGameView(game);
        }

        private void LoadGameView(GameType game)
        {
            if (game == GameType.StarRail)
            {
                HostFrame.Navigate(typeof(MiHoYoTools.Views.ToolViews.GachaView));
            }
            else
            {
                HostFrame.Navigate(typeof(MiHoYoTools.Modules.Zenless.Views.ToolViews.GachaView));
            }
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            GameContext.Current.GameChanged -= OnGameChanged;
            base.OnNavigatedFrom(e);
        }
    }
}

