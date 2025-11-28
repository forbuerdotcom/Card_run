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
    public partial class ShopView : UserControl
    {
        // Событие для сообщения главному окну, что нужно вернуться на карту
        public event Action ReturnToMap;

        private List<Card> _originalDeck = new List<Card>(); // Оригинальные карты (не изменяются)
        private List<Card> _playerDeck = new List<Card>(); // Карты для отображения (с улучшениями)
        private Dictionary<Card, int> _cardUpgrades = new Dictionary<Card, int>(); // Улучшения: ключ - оригинальная карта, значение - количество уровней
        private Dictionary<int, int> _cardUpgradesByIndex = new Dictionary<int, int>(); // Улучшения для сохранения: ключ - индекс карты, значение - количество уровней
        private Card _selectedCardForUpgrade = null; // Карта, выбранная для улучшения (оригинальная)
        private Dictionary<Card, CardListItemControl> _cardControls = new Dictionary<Card, CardListItemControl>();
        private GameState _gameState; // Состояние игры для доступа к золоту и счету
        private static readonly string UpgradesFilePath = "Data/CardUpgrades.json";

        public ShopView()
        {
            InitializeComponent();
            Loaded += ShopView_Loaded;
        }

        public void SetGameState(GameState gameState)
        {
            _gameState = gameState;
            UpdateGoldAndScore();
            // Сбрасываем выбранную карту при каждом заходе в магазин
            ResetDropZone();
        }

        private void ResetDropZone()
        {
            _selectedCardForUpgrade = null;
            DropZonePlaceholder.Visibility = Visibility.Visible;
            CardInDropZone.Visibility = Visibility.Collapsed;
            UpgradeButton.Visibility = Visibility.Collapsed;
        }

        private void ShopView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPlayerDeck();
            DrawDeckCards();
            UpdateGoldAndScore();
        }

        private void UpdateGoldAndScore()
        {
            if (_gameState != null)
            {
                GoldTextBlock.Text = _gameState.GoldEarned.ToString();
            }
        }

        private void LoadPlayerDeck()
        {
            // Загружаем оригинальные карты (не изменяем их)
            var loadedDeck = DeckManager.LoadDeck();
            _originalDeck = loadedDeck.Select(c => new Card(c)).ToList(); // Создаем копии оригинальных карт
            
            // Загружаем сохраненные улучшения
            LoadUpgrades();
            
            // Применяем улучшения к копиям для отображения
            ApplyUpgradesToDisplayDeck();
        }

        private void LoadUpgrades()
        {
            _cardUpgrades.Clear();
            _cardUpgradesByIndex.Clear();
            
            if (!System.IO.File.Exists(UpgradesFilePath))
                return;
            
            try
            {
                var json = System.IO.File.ReadAllText(UpgradesFilePath);
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                _cardUpgradesByIndex = System.Text.Json.JsonSerializer.Deserialize<Dictionary<int, int>>(json, options) ?? new Dictionary<int, int>();
                
                // Преобразуем индексы в ссылки на карты
                for (int i = 0; i < _originalDeck.Count && i < _cardUpgradesByIndex.Count; i++)
                {
                    if (_cardUpgradesByIndex.ContainsKey(i))
                    {
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

        private void SaveUpgrades()
        {
            try
            {
                // Преобразуем ссылки на карты в индексы
                _cardUpgradesByIndex.Clear();
                for (int i = 0; i < _originalDeck.Count; i++)
                {
                    if (_cardUpgrades.ContainsKey(_originalDeck[i]))
                    {
                        _cardUpgradesByIndex[i] = _cardUpgrades[_originalDeck[i]];
                    }
                }
                
                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                var json = System.Text.Json.JsonSerializer.Serialize(_cardUpgradesByIndex, options);
                
                var directory = System.IO.Path.GetDirectoryName(UpgradesFilePath);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
                
                System.IO.File.WriteAllText(UpgradesFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Не удалось сохранить улучшения: {ex.Message}");
            }
        }

        private void ApplyUpgradesToDisplayDeck()
        {
            _playerDeck.Clear();
            foreach (var originalCard in _originalDeck)
            {
                // Создаем копию оригинальной карты
                var displayCard = new Card(originalCard);
                
                // Применяем улучшения
                int levels = _cardUpgrades.ContainsKey(originalCard) ? _cardUpgrades[originalCard] : 0;
                ApplyUpgrades(displayCard, levels);
                
                _playerDeck.Add(displayCard);
            }
        }

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

        private void DrawDeckCards()
        {
            DeckCardsPanel.Children.Clear();
            _cardControls.Clear();

            for (int i = 0; i < _playerDeck.Count; i++)
            {
                var displayCard = _playerDeck[i];
                var originalCard = _originalDeck[i];
                
                var cardControl = new CardListItemControl();
                cardControl.SetCard(displayCard);
                cardControl.CardClicked += (card) => OnDeckCardClicked(originalCard); // Передаем оригинальную карту
                
                // Если карта была улучшена, меняем фон на светло-серый
                if (_cardUpgrades.ContainsKey(originalCard) && _cardUpgrades[originalCard] > 0)
                {
                    SetCardBackground(cardControl, new SolidColorBrush(Color.FromRgb(200, 200, 200)));
                }

                DeckCardsPanel.Children.Add(cardControl);
                _cardControls[originalCard] = cardControl;
            }
        }

        private void SetCardBackground(CardListItemControl cardControl, Brush brush)
        {
            // Находим Border в CardListItemControl и меняем его фон
            var border = FindVisualChild<Border>(cardControl);
            if (border != null)
            {
                border.Background = brush;
            }
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T)
                {
                    return (T)child;
                }
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        private void OnDeckCardClicked(Card originalCard)
        {
            // Проверяем, что карта принадлежит колоде игрока
            if (!_originalDeck.Contains(originalCard))
            {
                MessageBox.Show("Можно улучшать только карты из вашей колоды!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Помещаем карту в DropZone
            PlaceCardInDropZone(originalCard);
        }

        private void PlaceCardInDropZone(Card originalCard)
        {
            _selectedCardForUpgrade = originalCard;
            
            // Находим отображаемую версию карты (с улучшениями)
            int index = _originalDeck.IndexOf(originalCard);
            Card displayCard = index >= 0 && index < _playerDeck.Count ? _playerDeck[index] : originalCard;
            
            // Скрываем placeholder и показываем карту
            DropZonePlaceholder.Visibility = Visibility.Collapsed;
            CardInDropZone.Visibility = Visibility.Visible;

            // Обновляем информацию о карте в DropZone (показываем улучшенную версию)
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

        private int CalculateUpgradeCost(Card originalCard, int level)
        {
            // Формула: (10 + сила оригинальной карты) * 2^уровень карты
            return (10 + originalCard.Power) * (int)Math.Pow(2, level);
        }

        private void UpgradeButton_Click(object sender, RoutedEventArgs e)
        {
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

            // Уменьшаем золото
            _gameState.GoldEarned -= cost;

            // Увеличиваем уровень улучшения (не изменяем оригинальную карту!)
            if (!_cardUpgrades.ContainsKey(_selectedCardForUpgrade))
            {
                _cardUpgrades[_selectedCardForUpgrade] = 0;
            }
            _cardUpgrades[_selectedCardForUpgrade] += 1;

            // Применяем улучшения к отображаемым картам
            ApplyUpgradesToDisplayDeck();

            // Меняем фон карты на светло-серый
            if (_cardControls.ContainsKey(_selectedCardForUpgrade))
            {
                SetCardBackground(_cardControls[_selectedCardForUpgrade], new SolidColorBrush(Color.FromRgb(200, 200, 200)));
            }

            // Обновляем отображение карты в DropZone
            int index = _originalDeck.IndexOf(_selectedCardForUpgrade);
            Card displayCard = index >= 0 && index < _playerDeck.Count ? _playerDeck[index] : _selectedCardForUpgrade;
            DropZoneHP.Text = displayCard.CurrentHP.ToString();
            DropZoneDefence.Text = displayCard.Defence.ToString();
            DropZoneAD.Text = displayCard.AD.ToString();

            // Обновляем стоимость улучшения
            int newLevel = _cardUpgrades[_selectedCardForUpgrade];
            int newCost = CalculateUpgradeCost(_selectedCardForUpgrade, newLevel);
            UpgradeButton.Content = $"Улучшить: {newCost} золота";

            // Перерисовываем карты
            DrawDeckCards();
            
            // Сохраняем улучшения
            SaveUpgrades();
            
            // Обновляем отображение золота
            UpdateGoldAndScore();
        }

        private void DropZone_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            // Обработка drag & drop (если понадобится в будущем)
            e.Handled = true;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            // НЕ сохраняем улучшенные карты - улучшения только на сессию
            // Вызываем событие, чтобы закрыть магазин
            ReturnToMap?.Invoke();
        }

        // Метод для получения улучшенных карт для боя
        public List<Card> GetUpgradedDeck()
        {
            // Если колода еще не загружена, загружаем её
            if (_originalDeck.Count == 0)
            {
                LoadPlayerDeck();
            }
            
            var upgradedDeck = new List<Card>();
            foreach (var originalCard in _originalDeck)
            {
                var upgradedCard = new Card(originalCard);
                int levels = _cardUpgrades.ContainsKey(originalCard) ? _cardUpgrades[originalCard] : 0;
                ApplyUpgrades(upgradedCard, levels);
                upgradedDeck.Add(upgradedCard);
            }
            return upgradedDeck;
        }

        // Метод для сброса улучшений при начале новой игры
        public void ResetUpgrades()
        {
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
    }
}
