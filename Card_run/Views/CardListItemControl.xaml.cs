using System.Windows.Controls;
using System.Windows.Input;
using Card_run.BattleModels;

namespace Card_run.Views
{
    public partial class CardListItemControl : UserControl
    {
        public Card CardData { get; private set; }
        public event Action<Card> CardClicked;
        public event Action<Card> CardHovered;
        public event Action CardHoverEnded;

        public CardListItemControl()
        {
            InitializeComponent();
        }

        public void SetCard(Card card)
        {
            CardData = card;
            this.DataContext = card;
        }

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