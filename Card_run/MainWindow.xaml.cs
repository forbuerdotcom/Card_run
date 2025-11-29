using Card_run.BattleModels;
using Card_run.Models;
using Card_run.Views;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Card_run
{
    /// <summary>
    /// Главное окно приложения.
    /// Управляет переключением между различными представлениями (меню, карта, бой, магазин).
    /// Координирует взаимодействие между компонентами игры.
    /// </summary>
    public partial class MainWindow : Window
    {
        // Представление карты игры (граф с узлами)
        private GameMapView _gameMapView;
        
        // Представление боевой системы
        private BattleView _battleView;
        
        // Представление магазина для улучшения карт
        private ShopView _shopView;
        
        // Представление главного меню
        private MainMenu _mainMenu;
        
        // Представление подготовки колоды перед игрой
        private PreparationView _preparationView;
        
        // Текущее состояние игры (граф, позиция игрока, статистика)
        private GameState _gameState;
        
        // Искусственный интеллект для управления охотником (фиолетовая зона)
        private HunterAI _hunterAI;
        
        // Текущая команда врагов в бою
        private List<Card> _currentEnemyTeam;
        
        // Колода игрока (шаблон, используется для создания карт в бою)
        private List<Card> _playerDeck;

        /// <summary>
        /// Конструктор главного окна.
        /// Инициализирует все представления и подписывается на события.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            // Создаем все представления игры
            _mainMenu = new MainMenu();
            _gameMapView = new GameMapView();
            _battleView = new BattleView();
            _shopView = new ShopView();
            _preparationView = new PreparationView();
            _hunterAI = new HunterAI();

            // Подписываемся на события от представлений
            // Когда игрок хочет переместиться на карте
            _gameMapView.MovePlayerRequested += OnPlayerMoved;
            // Когда бой заканчивается
            _battleView.BattleEnded += OnBattleEnded;
            // Когда игрок выходит из магазина
            _shopView.ReturnToMap += OnShopExit;
            // Когда игрок готов начать игру с выбранной колодой
            _preparationView.StartGameRequested += OnStartGameRequested;
            // Когда игрок хочет вернуться в меню
            _preparationView.BackToMenuRequested += OnBackToMenuRequested;

            // Показываем главное меню при запуске
            MainContentControl.Content = _mainMenu;
        }

        /// <summary>
        /// Показывает главное меню.
        /// Используется для возврата в меню из других представлений.
        /// </summary>
        public void ShowMainMenu()
        {
            MainContentControl.Content = _mainMenu;
        }

        /// <summary>
        /// Обработчик нажатия клавиш.
        /// Escape - выход из приложения, Space - переход к подготовке колоды.
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            // Escape - выход из приложения
            if (e.Key == Key.Escape) Application.Current.Shutdown();
            // Space - переход к экрану подготовки колоды
            else if (e.Key == Key.Space) MainContentControl.Content = _preparationView;
        }

        /// <summary>
        /// Обработчик события начала игры.
        /// Создает новый граф, сбрасывает улучшения и инициализирует состояние игры.
        /// </summary>
        /// <param name="playerDeck">Колода игрока, выбранная на экране подготовки</param>
        private void OnStartGameRequested(List<Card> playerDeck)
        {
            // Сохраняем колоду игрока
            _playerDeck = playerDeck;
            
            // Сбрасываем улучшения при начале новой игры
            _shopView.ResetUpgrades();
            
            // Генерируем новый граф для игры
            var generator = new GraphGenerator();
            var graph = generator.Generate();
            
            // Создаем новое состояние игры с сгенерированным графом
            _gameState = new GameState(graph);
            
            // Устанавливаем состояние игры в представление карты
            _gameMapView.SetGameState(_gameState);
            
            // Переключаемся на представление карты
            MainContentControl.Content = _gameMapView;
        }

        /// <summary>
        /// Обработчик события возврата в меню.
        /// Переключает представление на главное меню.
        /// </summary>
        private void OnBackToMenuRequested()
        {
            MainContentControl.Content = _mainMenu;
        }

        /// <summary>
        /// Обработчик события перемещения игрока на карте.
        /// Обрабатывает логику перемещения, боев, магазина и расширения зоны охотника.
        /// </summary>
        /// <param name="destinationNode">Узел, на который игрок хочет переместиться</param>
        private void OnPlayerMoved(Node destinationNode)
        {
            // Пытаемся переместить игрока на выбранный узел
            bool moveSuccessful = _gameState.MovePlayer(destinationNode);

            if (moveSuccessful)
            {
                // Увеличиваем счетчик пройденных узлов для статистики
                _gameState.NodesVisited++;
                
                // Сбрасываем подсветку со всех узлов
                foreach (var node in _gameState.CurrentGraph.Nodes)
                {
                    node.IsPlayerCurrentPosition = false;
                }
                // Подсвечиваем новую позицию игрока
                _gameState.PlayerPosition.IsPlayerCurrentPosition = true;

                // --- ПРОВЕРКА НА БОЕВОЙ УЗЕЛ ---
                if (destinationNode.IsBattleNode)
                {
                    // Если узел уже очищен (враги побеждены) и не заражен охотником, пропускаем бой
                    if (destinationNode.IsCleared && !_gameState.HunterControlledNodes.Contains(destinationNode.Id))
                    {
                        // Узел уже очищен, просто продолжаем игру
                        ProcessHunterExpansion();
                        _gameMapView.SetGameState(_gameState);
                        CheckGameEnd();
                        return;
                    }
                    
                    // Если узел был очищен, но охотник его заразил, восстанавливаем врагов
                    if (destinationNode.IsCleared && _gameState.HunterControlledNodes.Contains(destinationNode.Id))
                    {
                        destinationNode.IsCleared = false; // Восстанавливаем врагов для повторного боя
                    }
                    
                    // Формируем команду врагов из ID, сохраненных в узле
                    var allEnemyCards = DataLoader.GetAllCards();
                    _currentEnemyTeam = destinationNode.EnemyTeamIds
                        .Select(id => new Card(allEnemyCards[id])) // Создаем копии карт врагов
                        .ToList();

                    // Если охотник уже контролирует этот узел, усиливаем врагов
                    if (_gameState.HunterControlledNodes.Contains(destinationNode.Id))
                    {
                        // Увеличиваем силу всех врагов на 1
                        foreach (var enemyCard in _currentEnemyTeam)
                        {
                            enemyCard.Strength += 1;
                        }
                    }

                    // Запускаем бой с улучшенными картами игрока
                    var upgradedPlayerDeck = _shopView.GetUpgradedDeck();
                    _battleView.StartBattle(_currentEnemyTeam, upgradedPlayerDeck);
                    MainContentControl.Content = _battleView;
                    return;
                }

                // Если это не боевой узел, проверяем другие типы узлов
                if (destinationNode.IsShop)
                {
                    // Если это магазин, переключаемся на представление магазина
                    _shopView.SetGameState(_gameState);
                    MainContentControl.Content = _shopView;
                }
                else
                {
                    // Обычный узел - обрабатываем расширение охотника и проверяем конец игры
                    ProcessHunterExpansion();
                    _gameMapView.SetGameState(_gameState);
                    CheckGameEnd();
                }
            }
        }

        /// <summary>
        /// Обработчик события выхода из магазина.
        /// Обрабатывает расширение охотника и проверяет конец игры.
        /// </summary>
        private void OnShopExit()
        {
            // Обрабатываем расширение зоны охотника после выхода из магазина
            ProcessHunterExpansion();
            // Обновляем состояние карты
            _gameMapView.SetGameState(_gameState);
            // Показываем карту
            ShowMap();
            // Проверяем, не закончилась ли игра
            CheckGameEnd();
        }

        /// <summary>
        /// Обрабатывает расширение зоны охотника (фиолетовая зона).
        /// Вызывает ИИ охотника для расчета следующего узла для захвата.
        /// </summary>
        private void ProcessHunterExpansion()
        {
            // Вызываем ИИ охотника для расчета следующего узла для захвата
            var nodeToInfect = _hunterAI.CalculateNextExpansion(_gameState);

            // Если ИИ вернул узел для захвата, расширяем колонию охотника
            if (nodeToInfect != null)
            {
                _gameState.ExpandHunterColony(nodeToInfect);
            }
        }

        /// <summary>
        /// Обработчик события окончания боя.
        /// Обновляет статистику, награждает золотом и обрабатывает результат боя.
        /// </summary>
        /// <param name="playerWon">Победил ли игрок в бою</param>
        /// <param name="defeatedEnemies">Список побежденных врагов</param>
        private void OnBattleEnded(bool playerWon, List<Card> defeatedEnemies)
        {
            if (playerWon)
            {
                // Показываем сообщение о победе
                MessageBox.Show("Вы одержали победу!");
                
                // Обновляем статистику победы: увеличиваем счетчик побежденных врагов
                _gameState.EnemiesDefeated += defeatedEnemies.Count;
                
                // Награждаем золотом: за каждого побежденного врага даем 10 золота
                _gameState.GoldEarned += defeatedEnemies.Count * 10;

                // Находим самого сильного из всех побежденных врагов для статистики
                foreach (var enemy in defeatedEnemies)
                {
                    if (_gameState.StrongestEnemyDefeated == null || enemy.Power > _gameState.StrongestEnemyDefeated.Power)
                    {
                        _gameState.StrongestEnemyDefeated = enemy;
                    }
                }
                
                // Помечаем текущий боевой узел как очищенный (враги побеждены)
                if (_gameState.PlayerPosition.IsBattleNode)
                {
                    _gameState.PlayerPosition.IsCleared = true;
                }
            }
            else
            {
                // Поражение - показываем экран окончания игры
                ShowGameOverScreen();
                return; // Возвращаемся, чтобы не идти дальше на карту
            }

            // Если игрок победил, возвращаемся на карту
            ShowMap();
            // Обрабатываем расширение зоны охотника
            ProcessHunterExpansion();
            // Обновляем состояние карты
            _gameMapView.SetGameState(_gameState);
            // Проверяем, не закончилась ли игра (достиг ли игрок финиша)
            CheckGameEnd();
        }

        /// <summary>
        /// Показывает экран окончания игры (поражение).
        /// Рассчитывает итоговый счет и отображает статистику.
        /// </summary>
        private void ShowGameOverScreen()
        {
            // Рассчитываем итоговый счет по формуле: (узлы * 10) + (враги * 5) + (золото)
            int finalScore = (_gameState.NodesVisited * 10) + (_gameState.EnemiesDefeated * 5) + _gameState.GoldEarned;

            // Получаем имя самого сильного побежденного врага (или "Нет", если не было)
            string strongestEnemyName = _gameState.StrongestEnemyDefeated?.Name ?? "Нет";

            // Создаем представление окончания игры со статистикой
            var gameOverView = new GameOverView(
                finalScore,
                _gameState.NodesVisited,
                _gameState.EnemiesDefeated,
                _gameState.GoldEarned,
                strongestEnemyName
            );

            // Переключаемся на представление окончания игры
            MainContentControl.Content = gameOverView;
        }

        /// <summary>
        /// Проверяет, не закончилась ли игра (достиг ли игрок финиша).
        /// Если игрок на финише, показывает экран победы.
        /// </summary>
        private void CheckGameEnd()
        {
            if (_gameState.PlayerPosition.IsFinish)
            {
                // Игрок достиг финиша - показываем экран победы со статистикой
                ShowVictoryScreen();
            }
        }

        /// <summary>
        /// Показывает экран победы.
        /// Рассчитывает итоговый счет и отображает статистику с заголовком "ПОБЕДА".
        /// </summary>
        private void ShowVictoryScreen()
        {
            // Рассчитываем итоговый счет по формуле: (узлы * 10) + (враги * 5) + (золото)
            int finalScore = (_gameState.NodesVisited * 10) + (_gameState.EnemiesDefeated * 5) + _gameState.GoldEarned;

            // Получаем имя самого сильного побежденного врага (или "Нет", если не было)
            string strongestEnemyName = _gameState.StrongestEnemyDefeated?.Name ?? "Нет";

            // Создаем представление окончания игры со статистикой
            var victoryView = new GameOverView(
                finalScore,
                _gameState.NodesVisited,
                _gameState.EnemiesDefeated,
                _gameState.GoldEarned,
                strongestEnemyName
            );
            
            // Меняем заголовок на "ПОБЕДА" вместо "ИГРА ОКОНЧЕНА"
            victoryView.SetTitle("ПОБЕДА");

            // Переключаемся на представление победы
            MainContentControl.Content = victoryView;
        }

        /// <summary>
        /// Показывает представление карты игры.
        /// Используется для возврата на карту после боя или выхода из магазина.
        /// </summary>
        private void ShowMap()
        {
            MainContentControl.Content = _gameMapView;
        }
    }
}
