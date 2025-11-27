// Путь: Models/GameState.cs
using System.Collections.Generic;
using System.Linq;

namespace Card_run.Models
{
    public class GameState
    {
        public Graph CurrentGraph { get; private set; }
        public Node PlayerPosition { get; private set; }
        public int PlayerMoveCount { get; private set; } = 0;

        // Колония "охотника" - множество зараженных клеток
        public HashSet<int> HunterControlledNodes { get; }

        public GameState(Graph graph)
        {
            CurrentGraph = graph;
            PlayerPosition = CurrentGraph.Nodes.First(n => n.IsPlayerStart);

            HunterControlledNodes = new HashSet<int>();
            // Находим стартовую клетку охотника и заражаем ее
            var hunterStart = CurrentGraph.Nodes.FirstOrDefault(n => n.IsHunter);
            if (hunterStart != null)
            {
                HunterControlledNodes.Add(hunterStart.Id);
            }
        }

        public bool MovePlayer(Node destinationNode)
        {
            // ИЗМЕНЕНИЕ: Убрана проверка, запрещающая ход в зараженную клетку.
            // Теперь игрок может ходить по фиолетовым зонам.

            bool isNeighbor = CurrentGraph.Edges.Contains((PlayerPosition.Id, destinationNode.Id)) ||
                              CurrentGraph.Edges.Contains((destinationNode.Id, PlayerPosition.Id));

            if (isNeighbor)
            {
                PlayerPosition = destinationNode;
                PlayerMoveCount++;
                destinationNode.IsVisitedByPlayer = true;

                // ИЗМЕНЕНИЕ: Если игрок зашел на клетку охотника, помечаем это.
                if (HunterControlledNodes.Contains(destinationNode.Id))
                {
                    destinationNode.IsVisitedByPlayerHunter = true;
                }

                return true;
            }
            return false;
        }

        public void ExpandHunterColony(Node newNode)
        {
            HunterControlledNodes.Add(newNode.Id);
        }

        public List<Node> GetAvailableMoves()
        {
            // ИЗМЕНЕНИЕ: Убрано исключение зараженных клеток из списка доступных ходов.
            return CurrentGraph.Nodes.Where(n =>
                (CurrentGraph.Edges.Contains((PlayerPosition.Id, n.Id)) ||
                 CurrentGraph.Edges.Contains((n.Id, PlayerPosition.Id)))
            ).ToList();
        }
    }
}