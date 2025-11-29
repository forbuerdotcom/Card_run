// Путь: Views/ShopView.xaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Card_run.BattleModels;
using Card_run.Models;

namespace Card_run.Views
{
    /// <summary>
    /// Представление магазина для улучшения карт.
    /// Позволяет игроку улучшать карты своей колоды за золото.
    /// Улучшения применяются ко всем характеристикам карты.
    /// </summary>
    public partial class ShopView : UserControl
    {
        // Событие для сообщения главному окну, что нужно вернуться на карту
        public event Action ReturnToMap;

        // Оригинальные карты колоды (не изменяются, используются как шаблоны)
        private List<Card> _originalDeck = new List<Card>();

        // Карты для отображения (с примененными улучшениями)
        internal List<Card> _playerDeck = new List<Card>();
        
        // Словарь улучшений: ключ - оригинальная карта, значение - количество уровней улучшения
        private Dictionary<Card, int> _cardUpgrades = new Dictionary<Card, int>();
        
        // Улучшения для сохранения: ключ - индекс карты, значение - количество уровней
        // Используется для сохранения в файл, так как ссылки на объекты нельзя сериализовать
        private Dictionary<int, int> _cardUpgradesByIndex = new Dictionary<int, int>();
        
        // Карта, выбранная для улучшения (оригинальная карта из _originalDeck)
        private Card _selectedCardForUpgrade = null;
        
        // Словарь для связи оригинальных карт с элементами управления
        private Dictionary<Card, CardListItemControl> _cardControls = new Dictionary<Card, CardListItemControl>();
        
        // Состояние игры для доступа к золоту и счету
        private GameState _gameState;

        // Путь к файлу с сохраненными улучшениями
        internal static string UpgradesFilePath = "Data/CardUpgrades.json";

        /// <summary>
        /// Конструктор представления магазина.
        /// Инициализирует компоненты и подписывается на событие загрузки.
        /// </summary>
        public ShopView()
        {
            InitializeComponent();
            Loaded += ShopView_Loaded;
        }

        /// <summary>
        /// Устанавливает состояние игры и обновляет отображение.
        /// Вызывается из главного окна при входе в магазин.
        /// </summary>
        public void SetGameState(GameState gameState)
        {
            // Сохраняем состояние игры для доступа к золоту
            _gameState = gameState;
            // Обновляем отображение золота и счета
            UpdateGoldAndScore();
            // Сбрасываем выбранную карту при каждом заходе в магазин
            ResetDropZone();
        }

        /// <summary>
        /// Сбрасывает зону выбора карты для улучшения.
        /// Скрывает выбранную карту и кнопку улучшения.
        /// </summary>
        private void ResetDropZone()
        {
            // Сбрасываем выбранную карту
            _selectedCardForUpgrade = null;
            // Показываем placeholder (текст-заглушку)
            DropZonePlaceholder.Visibility = Visibility.Visible;
            // Скрываем карту в зоне выбора
            CardInDropZone.Visibility = Visibility.Collapsed;
            // Скрываем кнопку улучшения
            UpgradeButton.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Обработчик события загрузки представления.
        /// Загружает колоду игрока и отображает карты.
        /// </summary>
        private void ShopView_Loaded(object sender, RoutedEventArgs e)
        {
            // Загружаем колоду игрока из файла
            LoadPlayerDeck();
            // Отображаем карты колоды
            DrawDeckCards();
            // Обновляем отображение золота и счета
            UpdateGoldAndScore();
        }

        /// <summary>
        /// Обновляет отображение золота и счета в правой панели.
        /// </summary>
        private void UpdateGoldAndScore()
        {
            if (_gameState != null)
            {
                // Отображаем текущее количество золота
                GoldTextBlock.Text = _gameState.GoldEarned.ToString();
            }
        }

        /// <summary>
        /// Загружает колоду игрока из файла.
        /// Создает копии оригинальных карт и применяет сохраненные улучшения.
        /// </summary>
        internal void LoadPlayerDeck()
        {
            // Загружаем оригинальные карты из файла (не изменяем их)
            var loadedDeck = DeckManager.LoadDeck();
            // Создаем копии оригинальных карт для работы
            _originalDeck = loadedDeck.Select(c => new Card(c)).ToList();
            
            // Загружаем сохраненные улучшения из файла
            LoadUpgrades();
            
            // Применяем улучшения к копиям для отображения
            ApplyUpgradesToDisplayDeck();
        }

        /// <summary>
        /// Загружает сохраненные улучшения из файла.
        /// Преобразует индексы карт в ссылки на объекты.
        /// </summary>
        private void LoadUpgrades()
        {
            // Очищаем словари улучшений
            _cardUpgrades.Clear();
            _cardUpgradesByIndex.Clear();
            
            // Проверяем, существует ли файл с улучшениями
            if (!System.IO.File.Exists(UpgradesFilePath))
                return;
            
            try
            {
                // Читаем JSON из файла
                var json = System.IO.File.ReadAllText(UpgradesFilePath);
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                // Десериализуем словарь с индексами
                _cardUpgradesByIndex = System.Text.Json.JsonSerializer.Deserialize<Dictionary<int, int>>(json, options) ?? new Dictionary<int, int>();
                
                // Преобразуем индексы в ссылки на карты
                for (int i = 0; i < _originalDeck.Count && i < _cardUpgradesByIndex.Count; i++)
                {
                    if (_cardUpgradesByIndex.ContainsKey(i))
                    {
                        // Связываем оригинальную карту с количеством уровней улучшения
                        _cardUpgrades[_originalDeck[i]] = _cardUpgradesByIndex[i];
                    }
                }
            }
            catch (Exception ex)
            {
                // Если не удалось загрузить, просто продолжаем без улучшений
                System.Diagnostics.Debug.WriteLine($"Не удалось загрузить улучшения: {ex.Message}");
            }
        }

        /// <summary>
        /// Сохраняет улучшения в файл.
        /// Преобразует ссылки на карты в индексы для сериализации.
        /// </summary>
        private void SaveUpgrades()
        {
            try
            {
                // Преобразуем ссылки на карты в индексы для сохранения
                _cardUpgradesByIndex.Clear();
                for (int i = 0; i < _originalDeck.Count; i++)
                {
                    if (_cardUpgrades.ContainsKey(_originalDeck[i]))
                    {
                        // Сохраняем индекс карты и количество уровней улучшения
                        _cardUpgradesByIndex[i] = _cardUpgrades[_originalDeck[i]];
                    }
                }
                
                // Сериализуем словарь в JSON
                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                var json = System.Text.Json.JsonSerializer.Serialize(_cardUpgradesByIndex, options);
                
                // Создаем директорию, если её нет
                var directory = System.IO.Path.GetDirectoryName(UpgradesFilePath);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
                
                // Записываем JSON в файл
                System.IO.File.WriteAllText(UpgradesFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Не удалось сохранить улучшения: {ex.Message}");
            }
        }

        /// <summary>
        /// Применяет сохраненные улучшения к отображаемым картам.
        /// Создает копии оригинальных карт с примененными улучшениями.
        /// </summary>
        private void ApplyUpgradesToDisplayDeck()
        {
            // Очищаем список отображаемых карт
            _playerDeck.Clear();
            
            // Проходим по всем оригинальным картам
            foreach (var originalCard in _originalDeck)
            {
                // Создаем копию оригинальной карты
                var displayCard = new Card(originalCard);
                
                // Получаем количество уровней улучшения для этой карты
                int levels = _cardUpgrades.ContainsKey(originalCard) ? _cardUpgrades[originalCard] : 0;
                // Применяем улучшения к копии
                ApplyUpgrades(displayCard, levels);
                
                // Добавляем улучшенную карту в список для отображения
                _playerDeck.Add(displayCard);
            }
        }

        /// <summary>
        /// Применяет улучшения к карте.
        /// Каждое улучшение добавляет +1 ко всем характеристикам карты.
        /// </summary>
        /// <param name="card">Карта, к которой применяются улучшения</param>
        /// <param name="levels">Количество уровней улучшения</param>
        private void ApplyUpgrades(Card card, int levels)
        {
            // Применяем улучшения: каждое улучшение добавляет +1 ко всем характеристикам
            card.MaxHP += levels;
            card.CurrentHP += levels;
            card.Speed += levels;
            card.Defence += levels;
            card.AD += levels;
            card.Power += levels;
        }

        /// <summary>
        /// Отображает карты колоды в нижней панели.
        /// Создает элементы управления для каждой карты и связывает их с оригинальными картами.
        /// </summary>
        private void DrawDeckCards()
        {
            // Очищаем панель и словарь элементов управления
            DeckCardsPanel.Children.Clear();
            _cardControls.Clear();

            // Проходим по всем картам колоды
            for (int i = 0; i < _playerDeck.Count; i++)
            {
                // Получаем отображаемую карту (с улучшениями)
                var displayCard = _playerDeck[i];
                // Получаем оригинальную карту (без улучшений)
                var originalCard = _originalDeck[i];
                
                // Создаем элемент управления для карты
                var cardControl = new CardListItemControl();
                cardControl.SetCard(displayCard);
                // Подписываемся на клик, передавая оригинальную карту
                cardControl.CardClicked += (card) => OnDeckCardClicked(originalCard);
                
                // Если карта была улучшена, меняем фон на светло-серый для визуального отличия
                if (_cardUpgrades.ContainsKey(originalCard) && _cardUpgrades[originalCard] > 0)
                {
                    SetCardBackground(cardControl, new SolidColorBrush(Color.FromRgb(200, 200, 200)));
                }

                // Добавляем элемент управления в панель
                DeckCardsPanel.Children.Add(cardControl);
                // Сохраняем связь между оригинальной картой и элементом управления
                _cardControls[originalCard] = cardControl;
            }
        }

        /// <summary>
        /// Устанавливает фон элемента управления картой.
        /// Используется для визуального отличия улучшенных карт.
        /// </summary>
        private void SetCardBackground(CardListItemControl cardControl, Brush brush)
        {
            // Находим Border в CardListItemControl и меняем его фон
            var border = FindVisualChild<Border>(cardControl);
            if (border != null)
            {
                border.Background = brush;
            }
        }

        /// <summary>
        /// Рекурсивно находит дочерний элемент указанного типа в визуальном дереве.
        /// Используется для поиска Border в CardListItemControl.
        /// </summary>
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            // Проходим по всем дочерним элементам
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // Если дочерний элемент нужного типа, возвращаем его
                if (child is T)
                {
                    return (T)child;
                }
                // Рекурсивно ищем в дочерних элементах
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        /// <summary>
        /// Обработчик клика по карте в колоде.
        /// Помещает карту в зону выбора для улучшения.
        /// </summary>
        private void OnDeckCardClicked(Card originalCard)
        {
            // Проверяем, что карта принадлежит колоде игрока
            if (!_originalDeck.Contains(originalCard))
            {
                MessageBox.Show("Можно улучшать только карты из вашей колоды!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Помещаем карту в зону выбора для улучшения
            PlaceCardInDropZone(originalCard);
        }

        /// <summary>
        /// Помещает карту в зону выбора для улучшения.
        /// Отображает информацию о карте и стоимость улучшения.
        /// </summary>
        internal void PlaceCardInDropZone(Card originalCard)
        {
            // Сохраняем выбранную карту
            _selectedCardForUpgrade = originalCard;
            
            // Находим отображаемую версию карты (с улучшениями)
            int index = _originalDeck.IndexOf(originalCard);
            Card displayCard = index >= 0 && index < _playerDeck.Count ? _playerDeck[index] : originalCard;
            
            // Скрываем placeholder и показываем карту
            DropZonePlaceholder.Visibility = Visibility.Collapsed;
            CardInDropZone.Visibility = Visibility.Visible;

            // Обновляем информацию о карте в зоне выбора (показываем улучшенную версию)
            DropZoneCardName.Text = displayCard.Name;
            DropZoneCardImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(displayCard.ImagePath, UriKind.RelativeOrAbsolute));
            DropZoneHP.Text = displayCard.CurrentHP.ToString();
            DropZoneDefence.Text = displayCard.Defence.ToString();
            DropZoneAD.Text = displayCard.AD.ToString();

            // Показываем кнопку улучшения с правильной стоимостью (используем оригинальную карту для расчета)
            int level = _cardUpgrades.ContainsKey(originalCard) ? _cardUpgrades[originalCard] : 0;
            int cost = CalculateUpgradeCost(originalCard, level);
            UpgradeButton.Content = $"Улучшить: {cost} золота";
            UpgradeButton.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Рассчитывает стоимость улучшения карты.
        /// Формула: (10 + сила оригинальной карты) * 2^уровень карты
        /// </summary>
        /// <param name="originalCard">Оригинальная карта (без улучшений)</param>
        /// <param name="level">Текущий уровень улучшения</param>
        /// <returns>Стоимость улучшения в золоте</returns>
        private int CalculateUpgradeCost(Card originalCard, int level)
        {
            // Формула: (10 + сила оригинальной карты) * 2^уровень карты
            // Стоимость экспоненциально растет с каждым уровнем
            return (10 + originalCard.Power) * (int)Math.Pow(2, level);
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Улучшить".
        /// Проверяет наличие золота, применяет улучшение и обновляет интерфейс.
        /// </summary>
        internal void UpgradeButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что карта выбрана и состояние игры установлено
            if (_selectedCardForUpgrade == null || _gameState == null)
                return;

            // Проверяем, что карта принадлежит игроку (находится в оригинальной колоде)
            if (!_originalDeck.Contains(_selectedCardForUpgrade))
            {
                MessageBox.Show("Можно улучшать только карты из вашей колоды!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Рассчитываем стоимость улучшения (используем оригинальную карту)
            int level = _cardUpgrades.ContainsKey(_selectedCardForUpgrade) ? _cardUpgrades[_selectedCardForUpgrade] : 0;
            int cost = CalculateUpgradeCost(_selectedCardForUpgrade, level);

            // Проверяем, достаточно ли золота
            if (_gameState.GoldEarned < cost)
            {
                MessageBox.Show($"Недостаточно золота! Нужно {cost}, у вас {_gameState.GoldEarned}.", "Недостаточно золота", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Уменьшаем золото на стоимость улучшения
            _gameState.GoldEarned -= cost;

            // Увеличиваем уровень улучшения (не изменяем оригинальную карту!)
            if (!_cardUpgrades.ContainsKey(_selectedCardForUpgrade))
            {
                _cardUpgrades[_selectedCardForUpgrade] = 0;
            }
            _cardUpgrades[_selectedCardForUpgrade] += 1;

            // Применяем улучшения к отображаемым картам
            ApplyUpgradesToDisplayDeck();

            // Меняем фон карты на светло-серый для визуального отличия
            if (_cardControls.ContainsKey(_selectedCardForUpgrade))
            {
                SetCardBackground(_cardControls[_selectedCardForUpgrade], new SolidColorBrush(Color.FromRgb(200, 200, 200)));
            }

            // Обновляем отображение карты в зоне выбора
            int index = _originalDeck.IndexOf(_selectedCardForUpgrade);
            Card displayCard = index >= 0 && index < _playerDeck.Count ? _playerDeck[index] : _selectedCardForUpgrade;
            DropZoneHP.Text = displayCard.CurrentHP.ToString();
            DropZoneDefence.Text = displayCard.Defence.ToString();
            DropZoneAD.Text = displayCard.AD.ToString();

            // Обновляем стоимость следующего улучшения
            int newLevel = _cardUpgrades[_selectedCardForUpgrade];
            int newCost = CalculateUpgradeCost(_selectedCardForUpgrade, newLevel);
            UpgradeButton.Content = $"Улучшить: {newCost} золота";

            // Перерисовываем карты в колоде
            DrawDeckCards();
            
            // Сохраняем улучшения в файл
            SaveUpgrades();
            
            // Обновляем отображение золота
            UpdateGoldAndScore();
        }

        /// <summary>
        /// Обработчик события перетаскивания над зоной выбора.
        /// Подготавливает зону для приема карты (если будет реализован drag & drop).
        /// </summary>
        private void DropZone_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        /// <summary>
        /// Обработчик события отпускания карты в зоне выбора.
        /// Обработка drag & drop (если будет реализован в будущем).
        /// </summary>
        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            // Обработка drag & drop (если понадобится в будущем)
            e.Handled = true;
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Выйти из магазина".
        /// Вызывает событие возврата на карту.
        /// </summary>
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            // Улучшения уже сохранены автоматически при каждом улучшении
            // Вызываем событие, чтобы закрыть магазин и вернуться на карту
            ReturnToMap?.Invoke();
        }

        /// <summary>
        /// Получает улучшенную колоду для использования в бою.
        /// Создает копии оригинальных карт с примененными улучшениями.
        /// </summary>
        /// <returns>Список улучшенных карт для боя</returns>
        public List<Card> GetUpgradedDeck()
        {
            // Если колода еще не загружена, загружаем её
            if (_originalDeck.Count == 0)
            {
                LoadPlayerDeck();
            }
            
            // Создаем список улучшенных карт
            var upgradedDeck = new List<Card>();
            foreach (var originalCard in _originalDeck)
            {
                // Создаем копию оригинальной карты
                var upgradedCard = new Card(originalCard);
                // Получаем количество уровней улучшения
                int levels = _cardUpgrades.ContainsKey(originalCard) ? _cardUpgrades[originalCard] : 0;
                // Применяем улучшения
                ApplyUpgrades(upgradedCard, levels);
                // Добавляем в список
                upgradedDeck.Add(upgradedCard);
            }
            return upgradedDeck;
        }

        /// <summary>
        /// Сбрасывает все улучшения при начале новой игры.
        /// Удаляет файл с улучшениями и перезагружает колоду.
        /// </summary>
        public void ResetUpgrades()
        {
            // Очищаем словари улучшений
            _cardUpgrades.Clear();
            _cardUpgradesByIndex.Clear();
            
            // Удаляем файл с улучшениями
            if (System.IO.File.Exists(UpgradesFilePath))
            {
                try
                {
                    System.IO.File.Delete(UpgradesFilePath);
                }
                catch { }
            }
            
            // Перезагружаем колоду, чтобы получить оригинальные значения
            LoadPlayerDeck();
            DrawDeckCards();
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Инструкция".
        /// Показывает модальное окно с подробной инструкцией по использованию магазина.
        /// </summary>
        private void ShowInstruction_Click(object sender, RoutedEventArgs e)
        {
            string instruction = @"ИНСТРУКЦИЯ ПО МАГАЗИНУ

1. ОБЩАЯ ИНФОРМАЦИЯ:
   - В магазине вы можете улучшать карты своей колоды за золото
   - Улучшения применяются ко всем характеристикам карты
   - Каждое улучшение добавляет +1 к здоровью, скорости, защите, атаке и силе
   - Улучшения сохраняются на весь забег
   - Улучшенные карты отображаются с серым фоном

2. КАК УЛУЧШИТЬ КАРТУ:
   - В нижней части экрана отображаются все карты вашей колоды
   - Кликните на карту, которую хотите улучшить
   - Карта появится в центральной зоне выбора
   - Нажмите кнопку 'Улучшить' с указанной стоимостью
   - Если у вас достаточно золота, улучшение будет применено

3. СТОИМОСТЬ УЛУЧШЕНИЯ:
   - Стоимость рассчитывается по формуле: (10 + Сила карты) * 2^Уровень
   - Первое улучшение стоит: (10 + Сила) * 1
   - Второе улучшение стоит: (10 + Сила) * 2
   - Третье улучшение стоит: (10 + Сила) * 4
   - И так далее - стоимость растет экспоненциально
   - Чем выше сила карты, тем дороже её улучшение

4. ОТОБРАЖЕНИЕ КАРТ:
   - Обычные карты отображаются с обычным фоном
   - Улучшенные карты отображаются с серым фоном
   - В зоне выбора отображаются текущие характеристики карты
   - После улучшения характеристики обновляются автоматически

5. ЗОЛОТО:
   - Золото отображается в правой панели
   - Золото получается за победу над врагами в боях
   - За каждого побежденного врага вы получаете 10 золота
   - Золото накапливается в течение всего забега

6. ХАРАКТЕРИСТИКИ КАРТ:
   - HP (Здоровье): количество урона, которое может выдержать карта
   - Defence (Защита): снижает получаемый урон
   - AD (Атака): базовый урон, наносимый картой
   - Каждое улучшение увеличивает все характеристики на +1

7. СТРАТЕГИЯ УЛУЧШЕНИЙ:
   - Улучшайте сильные карты для максимальной эффективности
   - Учитывайте, что стоимость растет экспоненциально
   - Лучше улучшить несколько карт понемногу, чем одну карту сильно
   - Улучшайте карты, которые часто используются в боях
   - Сохраняйте золото для важных улучшений

8. СОХРАНЕНИЕ УЛУЧШЕНИЙ:
   - Улучшения сохраняются автоматически при каждом улучшении
   - Улучшения сохраняются в файл CardUpgrades.json
   - Улучшения действуют на весь забег
   - При начале новой игры все улучшения сбрасываются

9. ВЫХОД ИЗ МАГАЗИНА:
   - Кнопка 'Выйти из магазина' возвращает вас на карту
   - Все улучшения уже сохранены
   - Вы можете вернуться в магазин в любое время, зайдя на желтый узел

10. ОГРАНИЧЕНИЯ:
    - Можно улучшать только карты из вашей колоды
    - Нельзя улучшить карту, если недостаточно золота
    - Нет ограничений на количество улучшений одной карты
    - Нет ограничений на общее количество улучшений

11. СОВЕТЫ:
    - Посещайте магазин регулярно для улучшения карт
    - Планируйте улучшения заранее
    - Учитывайте стоимость при выборе карт для улучшения
    - Улучшайте карты, которые хорошо работают в вашей стратегии";

            MessageBox.Show(instruction, "Инструкция по магазину", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
