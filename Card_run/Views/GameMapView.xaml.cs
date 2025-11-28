using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Card_run.Models;
using Card_run.BattleModels;

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
            UpdateGoldAndScore();
        }

        private void UpdateGoldAndScore()
        {
            if (_gameState != null)
            {
                GoldTextBlock.Text = _gameState.GoldEarned.ToString();
                // Формула счета: (узлы * 10) + (враги * 5) + (золото)
                int score = (_gameState.NodesVisited * 10) + (_gameState.EnemiesDefeated * 5) + _gameState.GoldEarned;
                ScoreTextBlock.Text = score.ToString();
            }
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
                bool isHunterControlled = _gameState.HunterControlledNodes.Contains(node.Id);

                // Определяем основной цвет узла
                Brush nodeBrush = Brushes.White;
                if (node.IsPlayerCurrentPosition) nodeBrush = Brushes.HotPink;
                else if (node.IsPlayerStart) nodeBrush = Brushes.LightGray;
                else if (node.IsShop) nodeBrush = Brushes.Yellow;
                else if (node.IsFinish) nodeBrush = Brushes.Green;
                // Приоритет у цвета охотника - проверяем его первым
                else if (isHunterControlled && node.IsVisitedByPlayerHunter)
                    nodeBrush = new SolidColorBrush(Color.FromRgb(200, 100, 200));
                else if (isHunterControlled)
                    nodeBrush = new SolidColorBrush(Color.FromRgb(128, 0, 128));
                else if (node.IsBattleNode)
                {
                    // Если боевой узел очищен, он серый (только если не заражен охотником)
                    if (node.IsCleared)
                    {
                        nodeBrush = Brushes.LightGray;
                    }
                    else
                    {
                        if (node.BattleDifficulty == BattleDifficulty.Weak) nodeBrush = Brushes.LimeGreen;
                        else if (node.BattleDifficulty == BattleDifficulty.Medium) nodeBrush = Brushes.IndianRed;
                        else if (node.BattleDifficulty == BattleDifficulty.Strong) nodeBrush = Brushes.Black;
                    }
                }
                else if (node.IsVisitedByPlayer) nodeBrush = Brushes.LightGray;

                // Создаем контейнер для всех частей узла
                var mainContainer = new Grid
                {
                    Width = 30,
                    Height = 30
                };

                // Если узел захвачен охотником, создаем составную фигуру
                if (isHunterControlled)
                {
                    // Создаем полукруг для основной части узла (левая половина)
                    var leftHalfPath = new Path { Fill = nodeBrush };
                    var leftHalfGeometry = new PathGeometry();
                    var leftHalfFigure = new PathFigure
                    {
                        StartPoint = new Point(15, 0),
                        IsClosed = true
                    };
                    leftHalfFigure.Segments.Add(new ArcSegment
                    {
                        Point = new Point(15, 30),
                        Size = new Size(15, 15),
                        SweepDirection = SweepDirection.Clockwise
                    });
                    // ИСПРАВЛЕНИЕ 1: Добавлен параметр 'true'
                    leftHalfFigure.Segments.Add(new LineSegment(new Point(15, 0), true));
                    leftHalfGeometry.Figures.Add(leftHalfFigure);
                    leftHalfPath.Data = leftHalfGeometry;

                    // Создаем полукруг для части охотника (правая половина)
                    var rightHalfPath = new Path { Fill = new SolidColorBrush(Color.FromRgb(128, 0, 128)) };
                    var rightHalfGeometry = new PathGeometry();
                    // ИСПРАВЛЕНИЕ 2: Обход проблемы с CounterClockwise
                    var rightHalfFigure = new PathFigure
                    {
                        StartPoint = new Point(15, 30), // Начинаем с нижнего центра
                        IsClosed = true
                    };
                    rightHalfFigure.Segments.Add(new ArcSegment
                    {
                        Point = new Point(15, 0), // Идем к верхнему центру
                        Size = new Size(15, 15),
                        SweepDirection = SweepDirection.Clockwise // Используем только Clockwise
                    });
                    // ИСПРАВЛЕНИЕ 1: Добавлен параметр 'true'
                    rightHalfFigure.Segments.Add(new LineSegment(new Point(15, 30), true));
                    rightHalfGeometry.Figures.Add(rightHalfFigure);
                    rightHalfPath.Data = rightHalfGeometry;

                    mainContainer.Children.Add(leftHalfPath);
                    mainContainer.Children.Add(rightHalfPath);
                }
                else
                {
                    // Для незахваченных узлов просто создаем круг
                    var fullCircle = new Ellipse
                    {
                        Width = 30,
                        Height = 30,
                        Fill = nodeBrush
                    };
                    mainContainer.Children.Add(fullCircle);
                }

                // Добавляем рамку поверх всего
                var ellipse = new Ellipse
                {
                    Width = 30,
                    Height = 30,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent // Прозрачная заливка, чтобы были видны фигуры под ней
                };
                mainContainer.Children.Add(ellipse);

                // Размещаем главный контейнер на канвасе
                Canvas.SetLeft(mainContainer, node.X * _scale + _offsetX - ellipse.Width / 2);
                Canvas.SetTop(mainContainer, node.Y * _scale + _offsetY - ellipse.Height / 2);

                // Логика кликов (привязана к эллипсу рамки)
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

                GraphCanvas.Children.Add(mainContainer);
            }
        }
    }
}