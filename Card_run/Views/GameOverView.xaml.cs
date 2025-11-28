using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Card_run.Views
{
    public partial class GameOverView : UserControl
    {
        public GameOverView(int score, int nodesVisited, int enemiesDefeated, int goldEarned, string strongestEnemyName)
        {
            InitializeComponent();

            // Устанавливаем значения в текстовые блоки
            ScoreTextBlock.Text = score.ToString();
            NodesVisitedTextBlock.Text = nodesVisited.ToString();
            EnemiesDefeatedTextBlock.Text = enemiesDefeated.ToString();
            GoldEarnedTextBlock.Text = goldEarned.ToString();
            StrongestEnemyTextBlock.Text = string.IsNullOrEmpty(strongestEnemyName) ? "Нет" : strongestEnemyName;
        }

        public void SetTitle(string title)
        {
            TitleTextBlock.Text = title;
            // Если это победа, меняем цвет на зеленый
            if (title == "ПОБЕДА")
            {
                TitleTextBlock.Foreground = new SolidColorBrush(Colors.LimeGreen);
            }
            else
            {
                TitleTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private void ReturnToMainMenu_Click(object sender, RoutedEventArgs e)
        {
            // Находим родительское окно и просим его показать главное меню
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.ShowMainMenu();
        }
    }
}