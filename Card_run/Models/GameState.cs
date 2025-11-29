// Путь: Models/GameState.cs
using Card_run.BattleModels;
using System.Collections.Generic;
using System.Linq;

namespace Card_run.Models
{
    /// <summary>
    /// Представляет текущее состояние игры.
    /// Хранит граф, позицию игрока, статистику и информацию о зоне охотника.
    /// </summary>
    public class GameState
    {
        // Текущий граф игры (карта с узлами и рёбрами)
        public Graph CurrentGraph { get; private set; }
        
        // Текущая позиция игрока на графе
        public Node PlayerPosition { get; private set; }

        // Счетчик ходов игрока (используется для определения начала расширения охотника)
        public int PlayerMoveCount { get; internal set; } = 0;
        
        // Количество посещенных узлов (для статистики и счета)
        public int NodesVisited { get; set; } = 0;
        
        // Количество побежденных врагов (для статистики и счета)
        public int EnemiesDefeated { get; set; } = 0;
        
        // Количество заработанного золота (для улучшения карт в магазине)
        public int GoldEarned { get; set; } = 0;
        
        // Самая сильная карта врага, которую победил игрок (для статистики)
        public Card StrongestEnemyDefeated { get; set; } = null;

        /// <summary>
        /// Колония "охотника" - множество ID узлов, захваченных охотником.
        /// Узлы охотника отображаются фиолетовым цветом на карте.
        /// </summary>
        public HashSet<int> HunterControlledNodes { get; }

        /// <summary>
        /// Конструктор состояния игры.
        /// Инициализирует граф, размещает игрока на стартовой точке и инициализирует зону охотника.
        /// </summary>
        /// <param name="graph">Граф игры с узлами и рёбрами</param>
        public GameState(Graph graph)
        {
            // Сохраняем граф игры
            CurrentGraph = graph;
            
            // Размещаем игрока на стартовой точке графа
            PlayerPosition = CurrentGraph.Nodes.First(n => n.IsPlayerStart);
            
            // Устанавливаем флаг, чтобы стартовая точка сразу окрасилась в цвет игрока на карте
            if (PlayerPosition != null)
            {
                PlayerPosition.IsPlayerCurrentPosition = true;
            }

            // Инициализируем множество захваченных охотником узлов
            HunterControlledNodes = new HashSet<int>();
            
            // Находим стартовую клетку охотника и добавляем её в колонию
            var hunterStart = CurrentGraph.Nodes.FirstOrDefault(n => n.IsHunter);
            if (hunterStart != null)
            {
                HunterControlledNodes.Add(hunterStart.Id);
            }
        }

        /// <summary>
        /// Перемещает игрока на указанный узел.
        /// Проверяет, что узел является соседом текущей позиции игрока.
        /// </summary>
        /// <param name="destinationNode">Узел, на который игрок хочет переместиться</param>
        /// <returns>true, если перемещение успешно, false, если узел не является соседом</returns>
        public bool MovePlayer(Node destinationNode)
        {
            if (destinationNode == null)
            {
                throw new ArgumentNullException(nameof(destinationNode));
            }
            // Проверяем, является ли целевой узел соседом текущей позиции игрока
            // Рёбра могут быть направленными или ненаправленными, поэтому проверяем оба варианта
            bool isNeighbor = CurrentGraph.Edges.Contains((PlayerPosition.Id, destinationNode.Id)) ||
                              CurrentGraph.Edges.Contains((destinationNode.Id, PlayerPosition.Id));

            if (isNeighbor)
            {
                // Перемещаем игрока на новый узел
                PlayerPosition = destinationNode;
                // Увеличиваем счетчик ходов
                PlayerMoveCount++;
                // Помечаем узел как посещенный игроком
                destinationNode.IsVisitedByPlayer = true;

                // Если игрок зашел на клетку охотника, помечаем это для визуального отображения
                if (HunterControlledNodes.Contains(destinationNode.Id))
                {
                    destinationNode.IsVisitedByPlayerHunter = true;
                }

                return true;
            }
            // Если узел не является соседом, перемещение невозможно
            return false;
        }

        /// <summary>
        /// Расширяет колонию охотника, добавляя новый узел.
        /// Если узел был очищен (враги побеждены), восстанавливает врагов.
        /// </summary>
        /// <param name="newNode">Новый узел для захвата охотником</param>
        public void ExpandHunterColony(Node newNode)
        {
            // Добавляем ID узла в множество захваченных охотником узлов
            HunterControlledNodes.Add(newNode.Id);
            
            // Если узел был очищен (враги побеждены), но охотник его заразил, восстанавливаем врагов
            // Это означает, что игроку придется сражаться с врагами снова
            if (newNode.IsCleared && newNode.IsBattleNode)
            {
                newNode.IsCleared = false;
            }
        }

        /// <summary>
        /// Получает список доступных для перемещения узлов.
        /// Возвращает все узлы, которые соединены рёбрами с текущей позицией игрока.
        /// </summary>
        /// <returns>Список узлов, на которые игрок может переместиться</returns>
        public List<Node> GetAvailableMoves()
        {
            // Находим все узлы, которые соединены рёбрами с текущей позицией игрока
            // Проверяем оба направления рёбер (направленные и ненаправленные)
            return CurrentGraph.Nodes.Where(n =>
                (CurrentGraph.Edges.Contains((PlayerPosition.Id, n.Id)) ||
                 CurrentGraph.Edges.Contains((n.Id, PlayerPosition.Id)))
            ).ToList();
        }
    }
}