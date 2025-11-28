// Models/HunterAI.cs
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Card_run.Models
{
    public class HunterAI
    {
        public Node CalculateNextExpansion(GameState state)
        {
            // Колония начинает расширяться после 2-го хода игрока
            if (state.PlayerMoveCount < 2) return null;

            var finishNode = state.CurrentGraph.Nodes.FirstOrDefault(n => n.IsFinish);
            if (finishNode == null) return null;

            // 1. Всегда находим "границу" колонии - клетки, куда можно расшириться.
            var frontier = GetColonyFrontier(state);
            if (!frontier.Any()) return null;

            // --- ФАЗА 1: Окружить финиш ---
            if (!IsFinishSurrounded(state, finishNode))
            {
                // 2. Ищем на границе клетки, которые являются соседями финиша.
                var finishNeighbors = GetNeighbors(state.CurrentGraph, finishNode);
                var frontierNeighborsOfFinish = frontier.Where(n => finishNeighbors.Contains(n)).ToList();

                if (frontierNeighborsOfFinish.Any())
                {
                    // 3. Если такие клетки есть, захватываем одну из них.
                    return frontierNeighborsOfFinish.First();
                }
                else
                {
                    // 4. Если клеток у финиша на границе нет, выбираем ту на границе, что ближе всего к финишу.
                    var closestToFrontier = frontier
                        .OrderBy(n => CalculateDistance(n, finishNode))
                        .FirstOrDefault();
                    return closestToFrontier;
                }
            }
            // --- ФАЗА 2: Захватить всю оставшуюся карту ---
            else
            {
                // Финиш окружен. Захватываем самые "выгодные" клетки на границе.
                var bestTarget = frontier
                    .OrderByDescending(n => CalculateExpansionProfit(state, n))
                    .FirstOrDefault();
                return bestTarget;
            }
        }

        #region Вспомогательные методы

        private bool IsFinishSurrounded(GameState state, Node finishNode)
        {
            var finishNeighbors = GetNeighbors(state.CurrentGraph, finishNode);
            return finishNeighbors.All(n => state.HunterControlledNodes.Contains(n.Id));
        }

        private List<Node> GetColonyFrontier(GameState state)
        {
            var frontier = new List<Node>();

            foreach (var controlledId in state.HunterControlledNodes)
            {
                var controlledNode = state.CurrentGraph.Nodes.First(n => n.Id == controlledId);
                var neighbors = GetNeighbors(state.CurrentGraph, controlledNode);

                foreach (var neighbor in neighbors)
                {
                    if (!state.HunterControlledNodes.Contains(neighbor.Id) && !neighbor.IsShop)
                    {
                        frontier.Add(neighbor);
                    }
                }
            }

            return frontier.Distinct().ToList();
        }

        private int CalculateExpansionProfit(GameState state, Node node)
        {
            int score = 100; // Базовая выгода
            if (node.IsVisitedByPlayer) score -= 50; // Штраф за клетки, где был игрок
            var distToPlayer = CalculateDistance(node, state.PlayerPosition);
            score += (int)(50 - distToPlayer); // Бонус за клетки ближе к игроку
            return score;
        }

        private double CalculateDistance(Node a, Node b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

        private List<Node> GetNeighbors(Graph graph, Node node)
        {
            return graph.Nodes.Where(n =>
                graph.Edges.Contains((node.Id, n.Id)) || graph.Edges.Contains((n.Id, node.Id))
            ).ToList();
        }

        #endregion
    }
}