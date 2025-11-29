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
    /// <summary>
    /// Представление карты игры в виде графа.
    /// Отображает узлы (точки на карте) и рёбра (связи между узлами).
    /// Позволяет игроку перемещаться по карте, кликая на соседние узлы.
    /// </summary>
    public partial class GameMapView : UserControl
    {
        // Текущее состояние игры, содержащее граф и позицию игрока
        private GameState _gameState;
        
        // Коэффициент масштабирования для отображения графа на экране
        private double _scale;
        
        // Смещение по оси X для центрирования графа на канвасе
        private double _offsetX;
        
        // Смещение по оси Y для центрирования графа на канвасе
        private double _offsetY;

        // Событие, которое вызывается, когда игрок хочет переместиться на узел
        public event Action<Node> MovePlayerRequested;

        /// <summary>
        /// Конструктор представления карты.
        /// Инициализирует компоненты и подписывается на изменение размера окна.
        /// </summary>
        public GameMapView()
        {
            InitializeComponent();
            // При изменении размера окна перерисовываем граф с новым масштабом
            this.SizeChanged += GameMapView_SizeChanged;
        }

        /// <summary>
        /// Устанавливает состояние игры и обновляет отображение.
        /// Вызывается из главного окна при переходе на карту.
        /// </summary>
        public void SetGameState(GameState gameState)
        {
            // Сохраняем состояние игры для использования в других методах
            _gameState = gameState;
            // Рисуем граф на канвасе
            DrawGraph();
            // Обновляем отображение золота и счета
            UpdateGoldAndScore();
        }

        /// <summary>
        /// Обновляет отображение золота и счета в правом верхнем углу.
        /// Счет рассчитывается по формуле: (узлы * 10) + (враги * 5) + (золото).
        /// </summary>
        private void UpdateGoldAndScore()
        {
            if (_gameState != null)
            {
                // Отображаем текущее количество золота
                GoldTextBlock.Text = _gameState.GoldEarned.ToString();
                
                // Рассчитываем счет по формуле: за каждый посещенный узел 10 очков,
                // за каждого побежденного врага 5 очков, плюс все золото
                int score = (_gameState.NodesVisited * 10) + (_gameState.EnemiesDefeated * 5) + _gameState.GoldEarned;
                ScoreTextBlock.Text = score.ToString();
            }
        }

        /// <summary>
        /// Обработчик изменения размера окна.
        /// При изменении размера перерисовываем граф с новым масштабом.
        /// </summary>
        private void GameMapView_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            // Перерисовываем граф только если состояние игры уже установлено
            if (_gameState != null) DrawGraph();
        }

        /// <summary>
        /// Рисует граф на канвасе.
        /// Сначала рисует рёбра (линии между узлами), затем узлы (кружки).
        /// </summary>
        private void DrawGraph()
        {
            // Очищаем канвас от предыдущих элементов
            GraphCanvas.Children.Clear();
            
            // Проверяем, что состояние игры и граф установлены
            if (_gameState?.CurrentGraph == null) return;

            var graph = _gameState.CurrentGraph;

            // --- ЛОГИКА МАСШТАБИРОВАНИЯ ---
            // Находим минимальные и максимальные координаты всех узлов
            var minX = graph.Nodes.Min(n => n.X);
            var maxX = graph.Nodes.Max(n => n.X);
            var minY = graph.Nodes.Min(n => n.Y);
            var maxY = graph.Nodes.Max(n => n.Y);
            
            // Вычисляем размеры графа в исходных координатах
            var graphWidth = maxX - minX;
            var graphHeight = maxY - minY;
            
            // Вычисляем доступное пространство на канвасе (75% от размера)
            double availableWidth = GraphCanvas.ActualWidth * 0.75;
            double availableHeight = GraphCanvas.ActualHeight * 0.75;
            
            // Вычисляем коэффициенты масштабирования для каждой оси
            double scaleX = availableWidth / graphWidth;
            double scaleY = availableHeight / graphHeight;
            
            // Используем меньший коэффициент, чтобы граф поместился по обеим осям
            _scale = Math.Min(scaleX, scaleY);
            
            // Вычисляем смещения для центрирования графа на канвасе
            _offsetX = (GraphCanvas.ActualWidth - graphWidth * _scale) / 2 - minX * _scale;
            _offsetY = (GraphCanvas.ActualHeight - graphHeight * _scale) / 2 - minY * _scale;

            // --- РИСОВАНИЕ РЁБЕР ---
            // Рёбра - это линии, соединяющие узлы графа
            foreach (var edge in graph.Edges)
            {
                // Находим узлы, которые соединяет это ребро
                var fromNode = graph.Nodes.First(n => n.Id == edge.From);
                var toNode = graph.Nodes.First(n => n.Id == edge.To);
                
                // Создаем линию с учетом масштаба и смещения
                Line line = new Line
                {
                    X1 = fromNode.X * _scale + _offsetX,
                    Y1 = fromNode.Y * _scale + _offsetY,
                    X2 = toNode.X * _scale + _offsetX,
                    Y2 = toNode.Y * _scale + _offsetY,
                    Stroke = Brushes.DarkGray,
                    StrokeThickness = 3
                };
                GraphCanvas.Children.Add(line);
            }

            // --- РИСОВАНИЕ ВЕРШИН (УЗЛОВ) ---
            foreach (var node in graph.Nodes)
            {
                // Проверяем, контролируется ли узел охотником (фиолетовая зона)
                bool isHunterControlled = _gameState.HunterControlledNodes.Contains(node.Id);

                // --- ОПРЕДЕЛЕНИЕ ЦВЕТА УЗЛА ---
                // Цвет узла зависит от его типа и состояния
                Brush nodeBrush = Brushes.White; // Цвет по умолчанию
                
                // Текущая позиция игрока - розовый цвет (высший приоритет)
                if (node.IsPlayerCurrentPosition) nodeBrush = Brushes.HotPink;
                // Стартовая позиция игрока - светло-серый
                else if (node.IsPlayerStart) nodeBrush = Brushes.LightGray;
                // Магазин - желтый цвет
                else if (node.IsShop) nodeBrush = Brushes.Yellow;
                // Финиш - зеленый цвет
                else if (node.IsFinish) nodeBrush = Brushes.Green;
                // Узел охотника, который игрок уже посетил - светло-фиолетовый
                else if (isHunterControlled && node.IsVisitedByPlayerHunter)
                    nodeBrush = new SolidColorBrush(Color.FromRgb(200, 100, 200));
                // Узел охотника, который игрок еще не посещал - темно-фиолетовый
                else if (isHunterControlled)
                    nodeBrush = new SolidColorBrush(Color.FromRgb(128, 0, 128));
                // Боевой узел - цвет зависит от сложности
                else if (node.IsBattleNode)
                {
                    // Если боевой узел очищен (враги побеждены), он серый
                    if (node.IsCleared)
                    {
                        nodeBrush = Brushes.LightGray;
                    }
                    else
                    {
                        // Цвет зависит от сложности боя:
                        // Слабый бой - зеленый
                        if (node.BattleDifficulty == BattleDifficulty.Weak) nodeBrush = Brushes.LimeGreen;
                        // Средний бой - красный
                        else if (node.BattleDifficulty == BattleDifficulty.Medium) nodeBrush = Brushes.IndianRed;
                        // Сильный бой - черный
                        else if (node.BattleDifficulty == BattleDifficulty.Strong) nodeBrush = Brushes.Black;
                    }
                }
                // Посещенный игроком узел - светло-серый
                else if (node.IsVisitedByPlayer) nodeBrush = Brushes.LightGray;

                // --- СОЗДАНИЕ ВИЗУАЛЬНОГО ПРЕДСТАВЛЕНИЯ УЗЛА ---
                // Создаем контейнер для всех частей узла
                var mainContainer = new Grid
                {
                    Width = 30,
                    Height = 30
                };

                // Если узел захвачен охотником, создаем составную фигуру (половина узла - фиолетовая)
                if (isHunterControlled)
                {
                    // Создаем левую половину узла (основной цвет)
                    var leftHalfPath = new Path { Fill = nodeBrush };
                    var leftHalfGeometry = new PathGeometry();
                    var leftHalfFigure = new PathFigure
                    {
                        StartPoint = new Point(15, 0), // Начинаем с верхнего центра
                        IsClosed = true
                    };
                    // Добавляем дугу, идущую от верхнего центра к нижнему центру
                    leftHalfFigure.Segments.Add(new ArcSegment
                    {
                        Point = new Point(15, 30), // Конечная точка - нижний центр
                        Size = new Size(15, 15), // Радиус дуги
                        SweepDirection = SweepDirection.Clockwise
                    });
                    // Добавляем прямую линию обратно к началу, замыкая фигуру
                    leftHalfFigure.Segments.Add(new LineSegment(new Point(15, 0), true));
                    leftHalfGeometry.Figures.Add(leftHalfFigure);
                    leftHalfPath.Data = leftHalfGeometry;

                    // Создаем правую половину узла (фиолетовый цвет охотника)
                    var rightHalfPath = new Path { Fill = new SolidColorBrush(Color.FromRgb(128, 0, 128)) };
                    var rightHalfGeometry = new PathGeometry();
                    var rightHalfFigure = new PathFigure
                    {
                        StartPoint = new Point(15, 30), // Начинаем с нижнего центра
                        IsClosed = true
                    };
                    // Добавляем дугу, идущую от нижнего центра к верхнему центру
                    rightHalfFigure.Segments.Add(new ArcSegment
                    {
                        Point = new Point(15, 0), // Конечная точка - верхний центр
                        Size = new Size(15, 15), // Радиус дуги
                        SweepDirection = SweepDirection.Clockwise
                    });
                    // Добавляем прямую линию обратно к началу, замыкая фигуру
                    rightHalfFigure.Segments.Add(new LineSegment(new Point(15, 30), true));
                    rightHalfGeometry.Figures.Add(rightHalfFigure);
                    rightHalfPath.Data = rightHalfGeometry;

                    // Добавляем обе половины в контейнер
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

                // Добавляем рамку поверх всего узла (черная обводка)
                var ellipse = new Ellipse
                {
                    Width = 30,
                    Height = 30,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent // Прозрачная заливка, чтобы были видны фигуры под ней
                };
                mainContainer.Children.Add(ellipse);

                // Размещаем главный контейнер на канвасе с учетом масштаба и смещения
                Canvas.SetLeft(mainContainer, node.X * _scale + _offsetX - ellipse.Width / 2);
                Canvas.SetTop(mainContainer, node.Y * _scale + _offsetY - ellipse.Height / 2);

                // --- ЛОГИКА КЛИКОВ ---
                // Проверяем, является ли узел соседом текущей позиции игрока
                bool isNeighbor = graph.Edges.Contains((_gameState.PlayerPosition.Id, node.Id)) ||
                                  graph.Edges.Contains((node.Id, _gameState.PlayerPosition.Id));

                if (isNeighbor)
                {
                    // Если узел соседний, делаем его кликабельным
                    ellipse.MouseLeftButtonDown += (sender, e) => MovePlayerRequested?.Invoke(node);
                    ellipse.Cursor = Cursors.Hand; // Меняем курсор на руку при наведении
                }
                else
                {
                    // Если узел не соседний, курсор остается обычной стрелкой
                    ellipse.Cursor = Cursors.Arrow;
                }

                // Добавляем контейнер узла на канвас
                GraphCanvas.Children.Add(mainContainer);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Инструкция".
        /// Показывает модальное окно с подробной инструкцией по использованию карты.
        /// </summary>
        private void ShowInstruction_Click(object sender, RoutedEventArgs e)
        {
            string instruction = @"ИНСТРУКЦИЯ ПО КАРТЕ ИГРЫ

1. ОБЩАЯ ИНФОРМАЦИЯ:
   - На экране отображается карта в виде графа
   - Узлы (кружки) - это точки на карте, между которыми можно перемещаться
   - Рёбра (линии) - это пути между узлами
   - Ваша цель - добраться до зеленого узла (финиш)

2. ТИПЫ УЗЛОВ И ИХ ЦВЕТА:
   - РОЗОВЫЙ - ваша текущая позиция
   - СВЕТЛО-СЕРЫЙ - стартовая позиция или уже посещенные узлы
   - ЖЕЛТЫЙ - магазин (здесь можно улучшить карты)
   - ЗЕЛЕНЫЙ - финиш (цель игры)
   - ФИОЛЕТОВЫЙ - узлы, захваченные охотником (опасная зона)
   - СВЕТЛО-ФИОЛЕТОВЫЙ - узлы охотника, которые вы уже посетили
   - ЗЕЛЕНЫЙ (боевой) - слабый бой (легкие враги)
   - КРАСНЫЙ (боевой) - средний бой (средние враги)
   - ЧЕРНЫЙ (боевой) - сильный бой (сильные враги)
   - СЕРЫЙ (боевой) - очищенный боевой узел (враги побеждены)

3. КАК ПЕРЕМЕЩАТЬСЯ:
   - Кликните левой кнопкой мыши на соседний узел (соединенный линией)
   - Курсор изменится на руку при наведении на доступный узел
   - Вы можете перемещаться только на соседние узлы
   - После перемещения на боевой узел начнется бой
   - После перемещения на магазин откроется магазин

4. ОХОТНИК (ФИОЛЕТОВАЯ ЗОНА):
   - Охотник начинает расширять свою зону после 3-го хода
   - Фиолетовые узлы - это зона охотника
   - Если вы зайдете на узел охотника, враги там будут усилены
   - Охотник стремится окружить финиш и захватить карту
   - Будьте осторожны при перемещении через фиолетовые зоны

5. БОЕВЫЕ УЗЛЫ:
   - Боевые узлы содержат врагов, с которыми нужно сразиться
   - Цвет узла показывает сложность боя
   - После победы узел становится серым (очищенным)
   - За каждого побежденного врага вы получаете золото
   - Если узел был очищен, но охотник его захватил, враги восстанавливаются

6. МАГАЗИН:
   - Желтый узел - это магазин
   - В магазине можно улучшить свои карты за золото
   - Улучшения сохраняются на весь забег
   - После выхода из магазина вы вернетесь на карту

7. СТАТИСТИКА:
   - В правом верхнем углу отображается ваше золото
   - Также отображается ваш текущий счет
   - Счет рассчитывается: (узлы * 10) + (враги * 5) + (золото)
   - Чем больше узлов вы посетите и врагов победите, тем выше счет

8. СТРАТЕГИЯ:
   - Планируйте маршрут к финишу заранее
   - Избегайте фиолетовых зон, если возможно
   - Очищайте боевые узлы для получения золота
   - Посещайте магазин для улучшения карт
   - Не задерживайтесь слишком долго - охотник расширяется

9. ПРАВИЛА ПОБЕДЫ И ПОРАЖЕНИЯ:
   - Доберитесь до зеленого узла (финиш) - вы победили
   - Проиграйте бой - игра заканчивается поражением
   - Ваш финальный счет отображается на экране результатов";

            MessageBox.Show(instruction, "Инструкция по карте", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}