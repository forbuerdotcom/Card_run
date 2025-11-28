// Путь: Models/GameState.cs
using Card_run.BattleModels;
using System.Collections.Generic;
using System.Linq;

namespace Card_run.Models
{
    public class GameState
    {
        public Graph CurrentGraph { get; private set; }
        public Node PlayerPosition { get; private set; }
        public int PlayerMoveCount { get; private set; } = 0;
        public int NodesVisited { get; set; } = 0;
        public int EnemiesDefeated { get; set; } = 0;
        public int GoldEarned { get; set; } = 0;
        public Card StrongestEnemyDefeated { get; set; } = null;

        // Колония "охотника" - множество зараженных клеток
        public HashSet<int> HunterControlledNodes { get; }

        public GameState(Graph graph)
        {
            CurrentGraph = graph;
            // Размещаем игрока на стартовой точке
            PlayerPosition = CurrentGraph.Nodes.First(n => n.IsPlayerStart);
            
            // Устанавливаем флаг, чтобы стартовая точка сразу окрасилась в цвет игрока
            if (PlayerPosition != null)
            {
                PlayerPosition.IsPlayerCurrentPosition = true;
            }

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
            bool isNeighbor = CurrentGraph.Edges.Contains((PlayerPosition.Id, destinationNode.Id)) ||
                              CurrentGraph.Edges.Contains((destinationNode.Id, PlayerPosition.Id));

            if (isNeighbor)
            {
                PlayerPosition = destinationNode;
                PlayerMoveCount++;
                destinationNode.IsVisitedByPlayer = true;

                // Если игрок зашел на клетку охотника, помечаем это.
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
            // Если узел был очищен, но охотник его заразил, восстанавливаем врагов
            if (newNode.IsCleared && newNode.IsBattleNode)
            {
                newNode.IsCleared = false;
            }
        }

        public List<Node> GetAvailableMoves()
        {
            return CurrentGraph.Nodes.Where(n =>
                (CurrentGraph.Edges.Contains((PlayerPosition.Id, n.Id)) ||
                 CurrentGraph.Edges.Contains((n.Id, PlayerPosition.Id)))
            ).ToList();
        }
    }
}