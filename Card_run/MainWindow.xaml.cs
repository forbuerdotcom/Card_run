using Card_run.BattleModels;
using Card_run.Models;
using Card_run.Views;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Card_run
{
    public partial class MainWindow : Window
    {
        private GameMapView _gameMapView;
        private BattleView _battleView;
        private ShopView _shopView;
        private MainMenu _mainMenu;
        private PreparationView _preparationView;
        private GameState _gameState;
        private HunterAI _hunterAI;
        private List<Card> _currentEnemyTeam;
        private List<Card> _playerDeck;

        public MainWindow()
        {
            InitializeComponent();
            
            _mainMenu = new MainMenu();
            _gameMapView = new GameMapView();
            _battleView = new BattleView();
            _shopView = new ShopView();
            _preparationView = new PreparationView();
            _hunterAI = new HunterAI();

            _gameMapView.MovePlayerRequested += OnPlayerMoved;
            _battleView.BattleEnded += OnBattleEnded;
            _shopView.ReturnToMap += OnShopExit;
            _preparationView.StartGameRequested += OnStartGameRequested;
            _preparationView.BackToMenuRequested += OnBackToMenuRequested;

            MainContentControl.Content = _mainMenu;
        }

        public void ShowMainMenu()
        {
            MainContentControl.Content = _mainMenu;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape) Application.Current.Shutdown();
            else if (e.Key == Key.Space) MainContentControl.Content = _preparationView;
        }

        private void OnStartGameRequested(List<Card> playerDeck)
        {
            _playerDeck = playerDeck;
            // Сбрасываем улучшения при начале новой игры
            _shopView.ResetUpgrades();
            var generator = new GraphGenerator();
            var graph = generator.Generate();
            _gameState = new GameState(graph);
            _gameMapView.SetGameState(_gameState);
            MainContentControl.Content = _gameMapView;
        }

        private void OnBackToMenuRequested()
        {
            MainContentControl.Content = _mainMenu;
        }

        private void OnPlayerMoved(Node destinationNode)
        {
            bool moveSuccessful = _gameState.MovePlayer(destinationNode);

            if (moveSuccessful)
            {
                // Увеличиваем счетчик пройденных узлов
                _gameState.NodesVisited++;
                // Сбрасываем подсветку со старой позиции
                foreach (var node in _gameState.CurrentGraph.Nodes)
                {
                    node.IsPlayerCurrentPosition = false;
                }
                _gameState.PlayerPosition.IsPlayerCurrentPosition = true;

                // ---  Проверка на боевой узел ---
                if (destinationNode.IsBattleNode)
                {
                    // Если узел уже очищен и не заражен охотником, пропускаем бой
                    if (destinationNode.IsCleared && !_gameState.HunterControlledNodes.Contains(destinationNode.Id))
                    {
                        // Узел уже очищен, просто продолжаем
                        ProcessHunterExpansion();
                        _gameMapView.SetGameState(_gameState);
                        CheckGameEnd();
                        return;
                    }
                    
                    // Если узел был очищен, но охотник его заразил, восстанавливаем врагов
                    if (destinationNode.IsCleared && _gameState.HunterControlledNodes.Contains(destinationNode.Id))
                    {
                        destinationNode.IsCleared = false; // Восстанавливаем врагов
                    }
                    
                    // Формируем команду врагов
                    var allEnemyCards = DataLoader.GetAllCards();
                    _currentEnemyTeam = destinationNode.EnemyTeamIds
                        .Select(id => new Card(allEnemyCards[id]))
                        .ToList();

                    // ЕСЛИ охотник уже контролирует этот узел, усиливаем врагов
                    if (_gameState.HunterControlledNodes.Contains(destinationNode.Id))
                    {
                        foreach (var enemyCard in _currentEnemyTeam)
                        {
                            enemyCard.Strength += 1;
                            MessageBox.Show($"Охотник усилил врагов в этой зоне! Сила '{enemyCard.Name}' теперь равна {enemyCard.Strength}.", "Усиление врагов", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }

                    // Запускаем бой с улучшенными картами игрока
                    var upgradedPlayerDeck = _shopView.GetUpgradedDeck();
                    _battleView.StartBattle(_currentEnemyTeam, upgradedPlayerDeck);
                    MainContentControl.Content = _battleView;
                    return;
                }

                // Если это не бой, продолжаем обычную логику
                if (destinationNode.IsShop)
                {
                    _shopView.SetGameState(_gameState);
                    MainContentControl.Content = _shopView;
                }
                else
                {
                    ProcessHunterExpansion();
                    _gameMapView.SetGameState(_gameState);
                    CheckGameEnd();
                }
            }
        }

        private void OnShopExit()
        {
            ProcessHunterExpansion();
            _gameMapView.SetGameState(_gameState);
            ShowMap();
            CheckGameEnd();
        }

        private void ProcessHunterExpansion()
        {

            var nodeToInfect = _hunterAI.CalculateNextExpansion(_gameState);

            if (nodeToInfect != null)
            {
                _gameState.ExpandHunterColony(nodeToInfect);
            }
            else
            {
            }
        }

        private void OnBattleEnded(bool playerWon, List<Card> defeatedEnemies)
        {
            if (playerWon)
            {
                MessageBox.Show("Вы одержали победу!");
                // Обновляем статистику победы
                _gameState.EnemiesDefeated += defeatedEnemies.Count;
                // За каждого врага даем золото (например, 10)
                _gameState.GoldEarned += defeatedEnemies.Count * 10;

                // Находим самого сильного из всех побежденных
                foreach (var enemy in defeatedEnemies)
                {
                    if (_gameState.StrongestEnemyDefeated == null || enemy.Power > _gameState.StrongestEnemyDefeated.Power)
                    {
                        _gameState.StrongestEnemyDefeated = enemy;
                    }
                }
                
                // Помечаем текущий боевой узел как очищенный
                if (_gameState.PlayerPosition.IsBattleNode)
                {
                    _gameState.PlayerPosition.IsCleared = true;
                }
            }
            else
            {
                // Поражение
                ShowGameOverScreen();
                return; // Возвращаемся, чтобы не идти дальше на карту
            }

            ShowMap();
            ProcessHunterExpansion();
            _gameMapView.SetGameState(_gameState);
            CheckGameEnd();
        }

        private void ShowGameOverScreen()
        {
            // Рассчитываем итоговый счет. Простая формула: (узлы * 10) + (враги * 5) + (золото)
            int finalScore = (_gameState.NodesVisited * 10) + (_gameState.EnemiesDefeated * 5) + _gameState.GoldEarned;

            string strongestEnemyName = _gameState.StrongestEnemyDefeated?.Name ?? "Нет";

            var gameOverView = new GameOverView(
                finalScore,
                _gameState.NodesVisited,
                _gameState.EnemiesDefeated,
                _gameState.GoldEarned,
                strongestEnemyName
            );

            MainContentControl.Content = gameOverView;
        }

        private void CheckGameEnd()
        {
            if (_gameState.PlayerPosition.IsFinish)
            {
                // Показываем экран победы со статистикой
                ShowVictoryScreen();
            }
        }

        private void ShowVictoryScreen()
        {
            // Рассчитываем итоговый счет. Простая формула: (узлы * 10) + (враги * 5) + (золото)
            int finalScore = (_gameState.NodesVisited * 10) + (_gameState.EnemiesDefeated * 5) + _gameState.GoldEarned;

            string strongestEnemyName = _gameState.StrongestEnemyDefeated?.Name ?? "Нет";

            var victoryView = new GameOverView(
                finalScore,
                _gameState.NodesVisited,
                _gameState.EnemiesDefeated,
                _gameState.GoldEarned,
                strongestEnemyName
            );
            
            // Меняем заголовок на "ПОБЕДА"
            victoryView.SetTitle("ПОБЕДА");

            MainContentControl.Content = victoryView;
        }

        private void ShowMap()
        {
            MainContentControl.Content = _gameMapView;
        }
    }
}