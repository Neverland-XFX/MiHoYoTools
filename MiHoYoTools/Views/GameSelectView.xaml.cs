using MiHoYoTools.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace MiHoYoTools.Views
{
    public sealed partial class GameSelectView : Page
    {
        public GameSelectView()
        {
            InitializeComponent();
            UpdateCurrentGameText(GameContext.Current.CurrentGame);
            GameContext.Current.GameChanged += OnGameChanged;
        }

        private void GameButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag
                && Enum.TryParse(tag, out GameType game))
            {
                GameContext.Current.SetGame(game);
                Frame?.Navigate(typeof(MainView));
                App.MainWindow?.SetSelectedNavItem("home");
            }
        }

        private void OnGameChanged(object sender, GameType game)
        {
            UpdateCurrentGameText(game);
        }

        private void UpdateCurrentGameText(GameType game)
        {
            CurrentGameText.Text = game == GameType.StarRail
                ? "当前选择：崩坏：星穹铁道"
                : "当前选择：绝区零";
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            GameContext.Current.GameChanged -= OnGameChanged;
            base.OnNavigatedFrom(e);
        }
    }
}

