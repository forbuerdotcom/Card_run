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
            _shopView.ReturnToMap += OnShopExit; // ИЗМЕНЕНИЕ: Создаем отдельный метод для выхода из магазина
            
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
                foreach (var node in _gameState.CurrentGraph.Nodes)
                {
                    node.IsPlayerCurrentPosition = false;
                }
                _gameState.PlayerPosition.IsPlayerCurrentPosition = true;

                if (_gameState.HunterControlledNodes.Contains(destinationNode.Id))
                {
                    MessageBox.Show("Ты вошел в зараженную зону!");
                }

                // ИЗМЕНЕНИЕ: Обработка хода и проверка концовки теперь вынесены за пределы if/else
                ProcessHunterExpansion();
                _gameMapView.SetGameState(_gameState); // Перерисовываем карту в любом случае

                // НОВОЕ: Проверяем, зашел ли игрок в магазин
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

        // НОВЫЙ МЕТОД: Вызывается при выходе из магазина
        private void OnShopExit()
        {
            ShowMap();
            // После выхода из магазина нужно проверить условия победы
            CheckGameEnd();
        }

        private void ProcessHunterExpansion()
        {
            var nodeToInfect = _hunterAI.CalculateNextExpansion(_gameState);
            if (nodeToInfect != null)
            {
                _gameState.ExpandHunterColony(nodeToInfect);
            }
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