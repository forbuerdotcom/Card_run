// Путь: Views/BattleView.xaml.cs
using System.Windows;
using System.Windows.Controls;

namespace Card_run.Views
{
    public partial class BattleView : UserControl
    {
        // Событие, которое будет сообщать главному окну, что нужно вернуться на карту
        public event Action ReturnToMap;

        public BattleView()
        {
            InitializeComponent();
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            // Вызываем событие, если кто-то на него подписан
            ReturnToMap?.Invoke();
        }
    }
}