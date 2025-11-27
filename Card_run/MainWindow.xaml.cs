// Путь: MainWindow.xaml.cs
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Card_run.Views;
using Card_run.Models;

namespace Card_run
{
    public partial class MainWindow : Window
    {
        private GameMapView _gameMapView;
        private BattleView _battleView;
        private ShopView _shopView;
        private MainMenu _mainMenu;
        private GameState _gameState;
        private HunterAI _hunterAI;

        public MainWindow()
        {
            InitializeComponent();
            
            _mainMenu = new MainMenu();
            _gameMapView = new GameMapView();
            _battleView = new BattleView();
            _shopView = new ShopView();
            _hunterAI = new HunterAI();

            _gameMapView.MovePlayerRequested += OnPlayerMoved;
            _battleView.ReturnToMap += ShowMap;
            _shopView.ReturnToMap += OnShopExit;
            
            MainContentControl.Content = _mainMenu;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape) Application.Current.Shutdown();
            else if (e.Key == Key.Space) StartNewGame();
        }

        private void StartNewGame()
        {
            var generator = new GraphGenerator();
            var graph = generator.Generate();
            _gameState = new GameState(graph);
            _gameMapView.SetGameState(_gameState);
            MainContentControl.Content = _gameMapView;
        }

        private void OnPlayerMoved(Node destinationNode)
        {
            bool moveSuccessful = _gameState.MovePlayer(destinationNode);

            if (moveSuccessful)
            {
                // Обновляем визуальное положение игрока
                foreach (var node in _gameState.CurrentGraph.Nodes)
                {
                    node.IsPlayerCurrentPosition = false;
                }
                _gameState.PlayerPosition.IsPlayerCurrentPosition = true;

                if (_gameState.HunterControlledNodes.Contains(destinationNode.Id))
                {
                    MessageBox.Show("Ты вошел в зараженную зону!");
                }

                // Вызываем обновленную логику хода охотника
                ProcessHunterExpansion();
                
                // Перерисовываем карту после хода охотника
                _gameMapView.SetGameState(_gameState);

                if (_gameState.PlayerPosition.IsShop)
                {
                    MainContentControl.Content = _shopView;
                }
                else
                {
                    CheckGameEnd();
                }
            }
        }

        /// <summary>
        /// Обрабатывает ход охотника, выбирая стратегию на основе состояния игры.
        /// </summary>
        private void ProcessHunterExpansion()
        {
            Node nodeToInfect = null;
            var finishNode = _gameState.CurrentGraph.Nodes.FirstOrDefault(n => n.IsFinish);

            // Проверяем, окружен ли финиш, чтобы выбрать правильную стратегию ИИ
            if (finishNode != null && IsFinishSurrounded(_gameState, finishNode))
            {
                // Фаза 2: Финиш окружен. Захватываем самые выгодные клетки на карте.
                nodeToInfect = _hunterAI.CalculateExpansionAfterFinishSurrounded(_gameState);
            }
            else
            {
                // Фаза 1: Финиш не окружен. Используем wp-калькулятор, чтобы его окружить.
                nodeToInfect = _hunterAI.CalculateWeakestPrecondition(_gameState);
            }
            
            if (nodeToInfect != null)
            {
                _gameState.ExpandHunterColony(nodeToInfect);
            }
        }

        /// <summary>
        /// Вспомогательный метод для проверки, окружен ли финиш.
        /// </summary>
        private bool IsFinishSurrounded(GameState state, Node finishNode)
        {
            if (finishNode == null) return true;
            var neighbors = state.CurrentGraph.Nodes.Where(n =>
                state.CurrentGraph.Edges.Contains((finishNode.Id, n.Id)) || state.CurrentGraph.Edges.Contains((n.Id, finishNode.Id))
            ).ToList();

            return neighbors.All(n => state.HunterControlledNodes.Contains(n.Id));
        }

        private void OnShopExit()
        {
            ShowMap();
            CheckGameEnd();
        }

        private void CheckGameEnd()
        {
            if (_gameState.PlayerPosition.IsFinish)
            {
                MessageBox.Show("Победа! Ты добрался до финиша!");
                StartNewGame();
            }
        }

        private void ShowMap()
        {
            MainContentControl.Content = _gameMapView;
        }
    }
}