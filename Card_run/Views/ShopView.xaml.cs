// Путь: Views/ShopView.xaml.cs
using System.Windows.Controls;

namespace Card_run.Views
{
    public partial class ShopView : UserControl
    {
        // Событие для сообщения главному окну, что нужно вернуться на карту
        public event Action ReturnToMap;

        public ShopView()
        {
            InitializeComponent();
        }

        private void ExitButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Вызываем событие, чтобы закрыть магазин
            ReturnToMap?.Invoke();
        }
    }
}