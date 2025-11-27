// Путь: Views/GameMapView.xaml.cs
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Card_run.Models;

namespace Card_run.Views
{
    public partial class GameMapView : UserControl
    {
        private GameState _gameState;
        private double _scale;
        private double _offsetX;
        private double _offsetY;

        public event Action<Node> MovePlayerRequested;

        public GameMapView()
        {
            InitializeComponent();
            this.SizeChanged += GameMapView_SizeChanged;
        }

        public void SetGameState(GameState gameState)
        {
            _gameState = gameState;
            DrawGraph();
        }

        private void GameMapView_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            if (_gameState != null) DrawGraph();
        }

        private void DrawGraph()
        {
            GraphCanvas.Children.Clear();
            if (_gameState?.CurrentGraph == null) return;

            var graph = _gameState.CurrentGraph;

            // --- Логика масштабирования (без изменений) ---
            var minX = graph.Nodes.Min(n => n.X); var maxX = graph.Nodes.Max(n => n.X);
            var minY = graph.Nodes.Min(n => n.Y); var maxY = graph.Nodes.Max(n => n.Y);
            var graphWidth = maxX - minX; var graphHeight = maxY - minY;
            double availableWidth = GraphCanvas.ActualWidth * 0.75;
            double availableHeight = GraphCanvas.ActualHeight * 0.75;
            double scaleX = availableWidth / graphWidth; double scaleY = availableHeight / graphHeight;
            _scale = Math.Min(scaleX, scaleY);
            _offsetX = (GraphCanvas.ActualWidth - graphWidth * _scale) / 2 - minX * _scale;
            _offsetY = (GraphCanvas.ActualHeight - graphHeight * _scale) / 2 - minY * _scale;

            // Рисуем ребра
            foreach (var edge in graph.Edges)
            {
                var fromNode = graph.Nodes.First(n => n.Id == edge.From);
                var toNode = graph.Nodes.First(n => n.Id == edge.To);
                Line line = new Line { X1 = fromNode.X * _scale + _offsetX, Y1 = fromNode.Y * _scale + _offsetY, X2 = toNode.X * _scale + _offsetX, Y2 = toNode.Y * _scale + _offsetY, Stroke = Brushes.DarkGray, StrokeThickness = 3 };
                GraphCanvas.Children.Add(line);
            }

            // Рисуем вершины
            foreach (var node in graph.Nodes)
            {
                Ellipse ellipse = new Ellipse { Width = 30, Height = 30, Stroke = Brushes.Black, StrokeThickness = 2 };
                
                // ИЗМЕНЕНИЕ: Обновлена логика окрашивания, стартовая точка теперь видна
                if (node.IsPlayerCurrentPosition) ellipse.Fill = Brushes.HotPink;
                else if (node.IsPlayerStart) ellipse.Fill = Brushes.LightGray; // НОВОЕ: Стартовая точка
                else if (node.IsShop) ellipse.Fill = Brushes.Yellow;
                else if (node.IsFinish) ellipse.Fill = Brushes.Green;
                else if (_gameState.HunterControlledNodes.Contains(node.Id) && node.IsVisitedByPlayerHunter)
                    ellipse.Fill = new SolidColorBrush(Color.FromRgb(200, 100, 200));
                else if (_gameState.HunterControlledNodes.Contains(node.Id))
                    ellipse.Fill = new SolidColorBrush(Color.FromRgb(128, 0, 128));
                else if (node.IsVisitedByPlayer) ellipse.Fill = Brushes.LightGray;
                else ellipse.Fill = Brushes.White;
                
                Canvas.SetLeft(ellipse, node.X * _scale + _offsetX - ellipse.Width / 2);
                Canvas.SetTop(ellipse, node.Y * _scale + _offsetY - ellipse.Height / 2);

                bool isNeighbor = graph.Edges.Contains((_gameState.PlayerPosition.Id, node.Id)) ||
                                  graph.Edges.Contains((node.Id, _gameState.PlayerPosition.Id));

                if (isNeighbor)
                {
                    ellipse.MouseLeftButtonDown += (sender, e) => MovePlayerRequested?.Invoke(node);
                    ellipse.Cursor = Cursors.Hand;
                }
                else
                {
                    ellipse.Cursor = Cursors.Arrow;
                }

                GraphCanvas.Children.Add(ellipse);
            }
        }
    }
}