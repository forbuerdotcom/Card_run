using System.Windows;
using System.Windows.Controls;

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

        private void ReturnToMainMenu_Click(object sender, RoutedEventArgs e)
        {
            // Находим родительское окно и просим его показать главное меню
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.ShowMainMenu();
        }
    }
}