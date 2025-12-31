using System;

namespace MiHoYoTools.Core
{
    public sealed class GameContext
    {
        private const string GameKey = "Config_CurrentGame";
        private GameType _currentGame = GameType.StarRail;

        public static GameContext Current { get; } = new GameContext();

        public event EventHandler<GameType> GameChanged;

        private GameContext()
        {
            var stored = AppLocalSettings.GetValue(GameKey, (int)GameType.StarRail);
            if (Enum.IsDefined(typeof(GameType), stored))
            {
                _currentGame = (GameType)stored;
            }
        }

        public GameType CurrentGame => _currentGame;

        public void SetGame(GameType game)
        {
            if (_currentGame == game)
            {
                return;
            }

            _currentGame = game;
            AppLocalSettings.SetValue(GameKey, (int)game);
            GameChanged?.Invoke(this, game);
        }
    }
}

