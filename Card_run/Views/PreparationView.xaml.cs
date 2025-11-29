using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Card_run.BattleModels;

namespace Card_run.Views
{
    /// <summary>
    /// Представление для подготовки колоды перед началом игры.
    /// Позволяет игроку выбрать до 5 карт с учетом ограничений по силе.
    /// </summary>
    public partial class PreparationView : UserControl
    {
        // Событие, которое вызывается, когда игрок готов начать игру с выбранной колодой
        public event Action<List<Card>> StartGameRequested;
        
        // Событие для возврата в главное меню
        public event Action BackToMenuRequested;

        // Список всех доступных карт в игре (загружается из файла)
        private List<Card> _allCards;

        // Колода игрока - карты, которые он выбрал для игры
        internal List<Card> _playerDeck = new List<Card>();

        /// <summary>
        /// Конструктор представления подготовки колоды.
        /// Инициализирует компоненты и подписывается на событие загрузки.
        /// </summary>
        public PreparationView()
        {
            InitializeComponent();
            // Подписываемся на событие загрузки, чтобы инициализировать данные после полной загрузки UI
            Loaded += PreparationView_Loaded;
        }

        /// <summary>
        /// Обработчик события загрузки представления.
        /// Загружает все доступные карты и инициализирует интерфейс.
        /// </summary>
        private void PreparationView_Loaded(object sender, RoutedEventArgs e)
        {
            // Загружаем все карты из файла данных
            _allCards = DataLoader.GetAllCards();
            // Инициализируем пустую колоду игрока
            _playerDeck = new List<Card>();

            // Отображаем все доступные карты в нижней панели
            DrawAllCards();
            // Обновляем отображение колоды игрока
            UpdateDeckUI();
            // Обновляем индикаторы ограничений колоды
            UpdateConstraintUI();
        }

        /// <summary>
        /// Отображает все доступные карты в панели выбора.
        /// Для каждой карты создается элемент управления, который можно кликнуть.
        /// </summary>
        private void DrawAllCards()
        {
            // Очищаем панель от предыдущих элементов
            AllCardsPanel.Children.Clear();
            
            // Проходим по всем доступным картам
            foreach (var card in _allCards)
            {
                // Создаем элемент управления для отображения карты
                var cardControl = new CardListItemControl();
                // Создаем копию карты, чтобы не изменять оригинальные данные
                cardControl.SetCard(new Card(card));
                // Подписываемся на события клика, наведения и ухода курсора
                cardControl.CardClicked += OnCardClicked;
                cardControl.CardHovered += OnCardHovered;
                cardControl.CardHoverEnded += OnCardHoverEnded;
                // Добавляем элемент управления в панель
                AllCardsPanel.Children.Add(cardControl);
            }
        }

        /// <summary>
        /// Обработчик клика по карте.
        /// Если карта уже в колоде - удаляет её, если нет - добавляет (с проверкой ограничений).
        /// </summary>
        internal void OnCardClicked(Card clickedCard)
        {
            // Проверяем, находится ли карта уже в колоде игрока
            bool cardIsInDeck = _playerDeck.Contains(clickedCard);

            if (cardIsInDeck)
            {
                // Если карта уже в колоде, удаляем её при клике
                _playerDeck.Remove(clickedCard);
            }
            else
            {
                // Если карты нет в колоде, создаем копию для добавления
                var cardToAdd = new Card(clickedCard);

                // Создаем временную копию колоды для проверки валидности
                var tempDeck = _playerDeck.ToList();
                tempDeck.Add(cardToAdd);
                
                // Проверяем, можно ли добавить карту с учетом всех ограничений
                if (IsDeckValid(tempDeck, out string errorMessage))
                {
                    // Если проверка прошла успешно, добавляем карту в колоду
                    _playerDeck.Add(cardToAdd);
                }
                else
                {
                    // Если проверка не прошла, показываем сообщение об ошибке
                    MessageBox.Show(errorMessage, "Невозможно добавить карту", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            // Обновляем отображение колоды и ограничений после изменения
            UpdateDeckUI();
            UpdateConstraintUI();
        }

        /// <summary>
        /// Обработчик наведения курсора на карту.
        /// Показывает подробную информацию о карте в правой панели.
        /// </summary>
        private void OnCardHovered(Card hoveredCard)
        {
            // Делаем панель информации видимой
            CardInfoPanel.Visibility = Visibility.Visible;
            
            // Заполняем все поля информации о карте
            InfoName.Text = hoveredCard.Name;
            InfoType.Text = $"Тип: {hoveredCard.Type}";
            InfoPower.Text = $"Сила: {hoveredCard.Power}";
            InfoHP.Text = $"Здоровье: {hoveredCard.CurrentHP}/{hoveredCard.MaxHP}";
            InfoAD.Text = $"Атака: {hoveredCard.AD}";
            InfoSpeed.Text = $"Скорость: {hoveredCard.Speed}";
            InfoDefence.Text = $"Защита: {hoveredCard.Defence}";
            
            // Преобразуем списки типов в строки для отображения
            string attackType = string.Join(", ", hoveredCard.AttackType);
            string defenceTypes = string.Join(", ", hoveredCard.DefenceTypes);
            string weaknesses = string.Join(", ", hoveredCard.DefenceWeaknesses);
            string DefenceMove = string.Join(", ", hoveredCard.DefenceMove);
            
            // Отображаем специальные характеристики карты
            InfoDefenceMove.Text = $"Защитное действие: {DefenceMove}";
            InfoAttackType.Text = $"Тип атаки: {attackType}";
            InfoDefenceType.Text = $"Тип защиты: {defenceTypes}";
            InfoDefenceWeaknesses.Text = $"Уязвим к: {weaknesses}";
        }

        /// <summary>
        /// Обработчик ухода курсора с карты.
        /// Скрывает панель с информацией о карте.
        /// </summary>
        private void OnCardHoverEnded()
        {
            // Скрываем панель информации, когда курсор уходит с карты
            CardInfoPanel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Обновляет отображение колоды игрока в верхней панели.
        /// Если колода пуста, показывает placeholder.
        /// </summary>
        private void UpdateDeckUI()
        {
            // Очищаем панель колоды от предыдущих элементов
            PlayerDeckPanel.Children.Clear();
            
            if (!_playerDeck.Any())
            {
                // Если колода пуста, показываем текст-заглушку
                DeckPlaceholder.Visibility = Visibility.Visible;
            }
            else
            {
                // Если в колоде есть карты, скрываем заглушку
                DeckPlaceholder.Visibility = Visibility.Collapsed;
                
                // Создаем элемент управления для каждой карты в колоде
                foreach (var card in _playerDeck)
                {
                    var cardControl = new CardListItemControl();
                    cardControl.SetCard(card);
                    // При клике на карту в колоде она удаляется
                    cardControl.CardClicked += OnCardClicked;
                    PlayerDeckPanel.Children.Add(cardControl);
                }
            }
            
            // Кнопка "Начать игру" активна только если в колоде есть хотя бы одна карта
            StartGameButton.IsEnabled = _playerDeck.Any();
        }

        /// <summary>
        /// Обновляет индикаторы ограничений колоды.
        /// Показывает текущее состояние по каждому ограничению.
        /// </summary>
        private void UpdateConstraintUI()
        {
            // Подсчитываем различные метрики колоды
            int deckCount = _playerDeck.Count; // Общее количество карт
            int highPowerCount = _playerDeck.Count(c => c.Power >= 9); // Карты с силой 9-10
            int midPowerCount = _playerDeck.Count(c => c.Power >= 7 && c.Power <= 8); // Карты с силой 7-8
            int totalPower = _playerDeck.Sum(c => c.Power); // Суммарная сила всех карт

            // Обновляем отображение каждого ограничения
            UpdateConstraintText(DeckCountText, deckCount, 5);
            UpdateConstraintText(HighPowerCountText, highPowerCount, 1);
            UpdateConstraintText(MidPowerCountText, midPowerCount, 3);
            UpdateConstraintText(TotalPowerText, totalPower, 35);
        }

        /// <summary>
        /// Обновляет текст ограничения и меняет цвет в зависимости от выполнения.
        /// Зеленый цвет означает, что ограничение выполнено или превышено.
        /// </summary>
        private void UpdateConstraintText(TextBlock textBlock, int current, int max)
        {
            // Форматируем текст как "текущее/максимальное"
            textBlock.Text = $"{current}/{max}";
            // Если текущее значение больше или равно максимальному, красим в зеленый
            textBlock.Foreground = current >= max ? Brushes.LimeGreen : Brushes.White;
        }

        /// <summary>
        /// Проверяет, соответствует ли колода всем ограничениям.
        /// Возвращает true, если колода валидна, иначе false с сообщением об ошибке.
        /// </summary>
        internal bool IsDeckValid(List<Card> deck, out string errorMessage)
        {
            errorMessage = null;

            // Ограничение 1: В колоде не может быть более 5 карт
            if (deck.Count > 5)
            {
                errorMessage = "В колоде не может быть более 5 карт.";
                return false;
            }
            
            // Ограничение 2: В колоде не может быть более одной карты с силой 9-10
            if (deck.Count(c => c.Power >= 9) > 1)
            {
                errorMessage = "В колоде не может быть более одной карты с силой 9-10.";
                return false;
            }
            
            // Ограничение 3: В колоде не может быть более трёх карт с силой 7-8
            if (deck.Count(c => c.Power >= 7 && c.Power <= 8) > 3)
            {
                errorMessage = "В колоде не может быть более трёх карт с силой 7-8.";
                return false;
            }
            
            // Ограничение 4: Суммарный показатель силы колоды не может превышать 35
            if (deck.Sum(c => c.Power) > 35)
            {
                errorMessage = "Суммарный показатель силы колоды не может превышать 35.";
                return false;
            }

            // Если все проверки прошли успешно, колода валидна
            return true;
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Начать игру".
        /// Сохраняет колоду и запускает игру, если колода не пуста.
        /// </summary>
        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            if (_playerDeck.Any())
            {
                // Сохраняем колоду в файл для последующего использования
                DeckManager.SaveDeck(_playerDeck);
                // Создаем копии карт и вызываем событие начала игры
                StartGameRequested?.Invoke(_playerDeck.Select(c => new Card(c)).ToList());
            }
            else
            {
                // Если колода пуста, показываем ошибку
                MessageBox.Show("Вы не можете начать игру с пустой колодой!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Очистить колоду".
        /// Запрашивает подтверждение и очищает колоду, если пользователь согласен.
        /// </summary>
        private void ClearDeck_Click(object sender, RoutedEventArgs e)
        {
            // Запрашиваем подтверждение у пользователя
            var result = MessageBox.Show("Вы уверены, что хотите очистить колоду?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Если пользователь подтвердил, очищаем колоду
                _playerDeck.Clear();

                // Обновляем интерфейс после очистки
                UpdateDeckUI();
                UpdateConstraintUI();
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "В меню".
        /// Вызывает событие возврата в главное меню.
        /// </summary>
        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            BackToMenuRequested?.Invoke();
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Инструкция".
        /// Показывает модальное окно с подробной инструкцией по использованию формы выбора карт.
        /// </summary>
        private void ShowInstruction_Click(object sender, RoutedEventArgs e)
        {
            string instruction = @"ИНСТРУКЦИЯ ПО ВЫБОРУ КАРТ

1. ОБЩАЯ ИНФОРМАЦИЯ:
   - На этом экране вы должны собрать свою колоду из доступных карт
   - Колода будет использоваться во всех боях во время забега
   - Выберите карты внимательно, так как от этого зависит ваш успех

2. КАК ВЫБРАТЬ КАРТУ:
   - В нижней части экрана отображаются все доступные карты
   - Наведите курсор на карту, чтобы увидеть её характеристики в правой панели
   - Кликните по карте, чтобы добавить её в вашу колоду (верхняя панель)
   - Кликните по карте в колоде, чтобы удалить её

3. ОГРАНИЧЕНИЯ КОЛОДЫ:
   - Максимум 5 карт в колоде (показатель вверху слева)
   - Не более 1 карты с силой 9-10 (очень сильные карты)
   - Не более 3 карт с силой 7-8 (сильные карты)
   - Суммарная сила всех карт не должна превышать 35

4. ИНДИКАТОРЫ ОГРАНИЧЕНИЙ:
   - В верхней части экрана отображаются 4 индикатора
   - Зеленый цвет означает, что ограничение выполнено
   - Белый цвет означает, что ограничение еще не выполнено
   - Следите за индикаторами при добавлении карт

5. ХАРАКТЕРИСТИКИ КАРТ:
   - Сила: общий показатель мощи карты (влияет на ограничения)
   - Здоровье: количество урона, которое может выдержать карта
   - Атака: базовый урон, наносимый картой
   - Защита: снижает получаемый урон
   - Скорость: определяет, как часто карта ходит в бою
   - Тип атаки: вид урона, который наносит карта
   - Тип защиты: к каким видам урона устойчива карта
   - Уязвимости: к каким видам урона карта уязвима
   - Защитное действие: специальное действие, которое может выполнить карта

6. СОВЕТЫ:
   - Создавайте сбалансированную колоду с разными типами карт
   - Учитывайте синергию между картами
   - Обращайте внимание на типы атак и защит
   - Не забывайте про защитные действия карт

7. НАЧАЛО ИГРЫ:
   - Кнопка 'Начать забег' активна только когда в колоде есть хотя бы одна карта
   - После нажатия колода сохраняется и начинается игра
   - Вы не сможете изменить колоду во время забега

8. ДОПОЛНИТЕЛЬНО:
   - Кнопка 'Очистить колоду' позволяет удалить все карты из колоды
   - Кнопка 'В меню' возвращает вас в главное меню
   - Все изменения колоды сохраняются автоматически";

            MessageBox.Show(instruction, "Инструкция по выбору карт", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}