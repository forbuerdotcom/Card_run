// Путь: Models/HunterAI.cs
using System.Collections.Generic;
using System.Linq;

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
            // Это гарантирует органический, пошаговый рост.
            var frontier = GetColonyFrontier(state);
            if (!frontier.Any()) return null;

            // --- ФАЗА 1: Окружить финиш ---
            // Проверяем, окружен ли финиш.
            if (!IsFinishSurrounded(state, finishNode))
            {
                // 2. Ищем на границе клетки, которые являются соседями финиша.
                var finishNeighbors = GetNeighbors(state.CurrentGraph, finishNode);
                var frontierNeighborsOfFinish = frontier.Where(n => finishNeighbors.Contains(n)).ToList();

                if (frontierNeighborsOfFinish.Any())
                {
                    // 3. Если такие клетки есть, захватываем одну из них.
                    // Колония "доползла" до финиша и начинает его окружать.
                    return frontierNeighborsOfFinish.First();
                }
                else
                {
                    // 4. Если клеток у финиша на границе нет, значит, мы еще не до него добрались.
                    // Выбираем ту клетку на границе, которая ближе всего к финишу.
                    // Это обеспечивает направленное, но пошаговое движение к цели.
                    var closestToFrontier = frontier
                        .OrderBy(n => CalculateDistance(n, finishNode))
                        .FirstOrDefault();
                    return closestToFrontier;
                }
            }
            // --- ФАЗА 2: Захватить всю оставшуюся карту ---
            else
            {
                // Финиш окружен. Теперь просто захватываем самые "выгодные" клетки на границе.
                var bestTarget = frontier
                    .OrderByDescending(n => CalculateExpansionProfit(state, n))
                    .FirstOrDefault();
                return bestTarget;
            }
        }

        // --- Вспомогательные методы ---

        /// <summary>
        /// Проверяет, окружен ли финиш клетками охотника.
        /// </summary>
        private bool IsFinishSurrounded(GameState state, Node finishNode)
        {
            var finishNeighbors = GetNeighbors(state.CurrentGraph, finishNode);
            // Финиш окружен, если все его соседи принадлежат охотнику.
            return finishNeighbors.All(n => state.HunterControlledNodes.Contains(n.Id));
        }

        /// <summary>
        /// Возвращает список "граничных" клеток - соседей колонии, которые еще не захвачены.
        /// Это ключ к органическому росту. Магазин никогда не будет целью для захвата.
        /// </summary>
        private List<Node> GetColonyFrontier(GameState state)
        {
            var frontier = new List<Node>();
            // Проходим по всем захваченным клеткам
            foreach (var controlledId in state.HunterControlledNodes)
            {
                var controlledNode = state.CurrentGraph.Nodes.First(n => n.Id == controlledId);
                var neighbors = GetNeighbors(state.CurrentGraph, controlledNode);
                // Если сосед не захвачен и не является магазином, он - часть границы
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

        /// <summary>
        /// Вычисляет "выгоду" захвата клетки для второй фазы игры.
        /// </summary>
        private int CalculateExpansionProfit(GameState state, Node node)
        {
            int score = 100; // Базовая выгода

            // Штраф за клетки, где был игрок.
            if (node.IsVisitedByPlayer)
            {
                score -= 50;
            }

            // Бонус за клетки ближе к игроку, чтобы мешать ему.
            var distToPlayer = CalculateDistance(node, state.PlayerPosition);
            score += (int)(50 - distToPlayer);

            return score;
        }

        // Простое вычисление расстояния между двумя узлами.
        private double CalculateDistance(Node a, Node b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

        // Получение соседей узла.
        private List<Node> GetNeighbors(Graph graph, Node node)
        {
            return graph.Nodes.Where(n =>
                graph.Edges.Contains((node.Id, n.Id)) || graph.Edges.Contains((n.Id, node.Id))
            ).ToList();
        }
    }
}