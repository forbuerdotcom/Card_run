using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Card_run.BattleModels;

namespace Card_run.Views
{
    public partial class BattleCardControl : UserControl
    {
        public Card CardData { get; private set; }

        // События
        public event Action<Card> CardClicked;
        public event Action<Card> CardHovered;
        public event Action CardHoverEnded;
        public event Action<Card> CardDied; // Новое событие для гибели карты

        public BattleCardControl()
        {
            InitializeComponent();
        }

        public void SetCard(Card card)
        {
            // Отписываемся от старой карты, чтобы избежать утечек памяти
            if (CardData != null)
            {
                CardData.PropertyChanged -= OnCardPropertyChanged;
            }

            CardData = card;
            this.DataContext = card;

            // Подписываемся на изменения свойств новой карты
            if (CardData != null)
            {
                CardData.PropertyChanged += OnCardPropertyChanged;
            }
        }

        private void OnCardPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Проверяем, изменилось ли свойство Status и стало ли оно равным Dead
            if (e.PropertyName == nameof(Card.Status) && CardData.Status == CardStatus.Dead)
            {
                // Вызываем событие гибели карты
                CardDied?.Invoke(CardData);
            }
        }

        // Обработчики событий UI
        private void Card_MouseEnter(object sender, MouseEventArgs e)
        {
            CardHovered?.Invoke(CardData);
        }

        private void Card_MouseLeave(object sender, MouseEventArgs e)
        {
            CardHoverEnded?.Invoke();
        }

        private void Card_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CardClicked?.Invoke(CardData);
        }
    }
}