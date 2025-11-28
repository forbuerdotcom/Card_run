using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Card_run.BattleModels;

namespace Card_run.Views
{
    public partial class PreparationView : UserControl
    {
        public event Action<List<Card>> StartGameRequested;
        public event Action BackToMenuRequested;

        private List<Card> _allCards;
        private List<Card> _playerDeck = new List<Card>();

        public PreparationView()
        {
            InitializeComponent();
            Loaded += PreparationView_Loaded;
        }

        private void PreparationView_Loaded(object sender, RoutedEventArgs e)
        {
            _allCards = DataLoader.GetAllCards();
            DrawAllCards();
            UpdateConstraintUI();
        }

        private void DrawAllCards()
        {
            AllCardsPanel.Children.Clear();
            foreach (var card in _allCards)
            {
                var cardControl = new CardListItemControl();
                cardControl.SetCard(new Card(card)); // Создаем копию
                cardControl.CardClicked += OnCardClicked;
                cardControl.CardHovered += OnCardHovered;
                cardControl.CardHoverEnded += OnCardHoverEnded;
                AllCardsPanel.Children.Add(cardControl);
            }
        }

        private void OnCardClicked(Card clickedCard)
        {
            bool cardIsInDeck = _playerDeck.Contains(clickedCard);

            if (cardIsInDeck)
            {
                _playerDeck.Remove(clickedCard);
            }
            else
            {
                var cardToAdd = new Card(clickedCard);

                var tempDeck = _playerDeck.ToList();
                tempDeck.Add(cardToAdd);
                if (IsDeckValid(tempDeck, out string errorMessage))
                {
                    _playerDeck.Add(cardToAdd);
                }
                else
                {
                    MessageBox.Show(errorMessage, "Невозможно добавить карту", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            UpdateDeckUI();
            UpdateConstraintUI();
        }

        private void OnCardHovered(Card hoveredCard)
        {
            CardInfoPanel.Visibility = Visibility.Visible;
            InfoName.Text = hoveredCard.Name;
            InfoType.Text = $"Тип: {hoveredCard.Type}";
            InfoPower.Text = $"Сила: {hoveredCard.Power}";
            InfoHP.Text = $"Здоровье: {hoveredCard.CurrentHP}/{hoveredCard.MaxHP}";
            InfoAD.Text = $"Атака: {hoveredCard.AD}";
            InfoSpeed.Text = $"Скорость: {hoveredCard.Speed}";
            InfoDefence.Text = $"Защита: {hoveredCard.Defence}";
            InfoPerk.Text = hoveredCard.PerkName;
        }

        private void OnCardHoverEnded()
        {
            CardInfoPanel.Visibility = Visibility.Collapsed;
        }

        private void UpdateDeckUI()
        {
            PlayerDeckPanel.Children.Clear();
            if (!_playerDeck.Any())
            {
                DeckPlaceholder.Visibility = Visibility.Visible;
            }
            else
            {
                DeckPlaceholder.Visibility = Visibility.Collapsed;
                foreach (var card in _playerDeck)
                {
                    var cardControl = new CardListItemControl();
                    cardControl.SetCard(card);
                    cardControl.CardClicked += OnCardClicked; // Клик по карте в колоде удаляет её
                    PlayerDeckPanel.Children.Add(cardControl);
                }
            }
            StartGameButton.IsEnabled = _playerDeck.Any();
        }

        private void UpdateConstraintUI()
        {
            int deckCount = _playerDeck.Count;
            int highPowerCount = _playerDeck.Count(c => c.Power >= 9);
            int midPowerCount = _playerDeck.Count(c => c.Power >= 7 && c.Power <= 8);
            int totalPower = _playerDeck.Sum(c => c.Power);

            UpdateConstraintText(DeckCountText, deckCount, 5);
            UpdateConstraintText(HighPowerCountText, highPowerCount, 1);
            UpdateConstraintText(MidPowerCountText, midPowerCount, 3);
            UpdateConstraintText(TotalPowerText, totalPower, 35);
        }

        private void UpdateConstraintText(TextBlock textBlock, int current, int max)
        {
            textBlock.Text = $"{current}/{max}";
            textBlock.Foreground = current >= max ? Brushes.LimeGreen : Brushes.White;
        }

        private bool IsDeckValid(List<Card> deck, out string errorMessage)
        {
            errorMessage = null;

            if (deck.Count > 5)
            {
                errorMessage = "В колоде не может быть более 5 карт.";
                return false;
            }
            if (deck.Count(c => c.Power >= 9) > 1)
            {
                errorMessage = "В колоде не может быть более одной карты с силой 9-10.";
                return false;
            }
            if (deck.Count(c => c.Power >= 7 && c.Power <= 8) > 3)
            {
                errorMessage = "В колоде не может быть более трёх карт с силой 7-8.";
                return false;
            }
            if (deck.Sum(c => c.Power) > 35)
            {
                errorMessage = "Суммарный показатель силы колоды не может превышать 35.";
                return false;
            }

            return true;
        }

        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            if (_playerDeck.Any())
            {
                StartGameRequested?.Invoke(_playerDeck.Select(c => new Card(c)).ToList());
            }
            else
            {
                MessageBox.Show("Вы не можете начать игру с пустой колодой!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            BackToMenuRequested?.Invoke();
        }
    }
}