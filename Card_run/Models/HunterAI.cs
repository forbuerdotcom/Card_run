// Путь: Models/HunterAI.cs
using System.Collections.Generic;
using System.Linq;

namespace Card_run.Models
{
    public class HunterAI
    {
        /// <summary>
        /// Вычисляет следующий ход на основе логики wp-калькулятора.
        /// </summary>
        /// <param name="state">Текущее состояние игры (Pre).</param>
        /// <returns>Узел для захвата, который является вычисленным предусловием для достижения цели.</returns>
        public Node CalculateWeakestPrecondition(GameState state)
        {
            // Колония начинает расширяться после 2-го хода игрока
            if (state.PlayerMoveCount < 2) return null;

            var finishNode = state.CurrentGraph.Nodes.FirstOrDefault(n => n.IsFinish);
            if (finishNode == null) return null;

            // --- Этап 1: Определение Постусловия (Post) ---
            // Главная цель ИИ на первом этапе: окружить финиш.
            // Post: IsFinishSurrounded(state, finishNode) == true

            // --- Этап 2: Вычисление wp для достижения Post ---
            // Чтобы окружить финиш (Post), необходимо захватить всех его соседей.
            // wp(Post) = { n | n is neighbor of finishNode AND n is not controlled }
            var uncontrolledFinishNeighbors = GetNeighbors(state.CurrentGraph, finishNode)
                                                .Where(n => !state.HunterControlledNodes.Contains(n.Id) && !n.IsShop)
                                                .ToList();

            // --- Этап 3: Проверка достижимости wp из текущего состояния ---
            // Мы можем захватить клетку, только если она находится на границе колонии.
            // frontier = { n | n is neighbor of controlled cell AND n is not controlled }
            var frontier = GetColonyFrontier(state);

            // --- Этап 4: Вычисление итогового Предусловия (Pre) ---
            // Итоговое действие (Pre) - это пересечение множества клеток для захвата (wp) и границы колонии (frontier).
            // Pre = wp(Post) ∩ frontier
            var actionTargets = uncontrolledFinishNeighbors.Intersect(frontier).ToList();

            if (actionTargets.Any())
            {
                // Предусловие выполнено: мы можем захватить соседа финиша.
                // Это самый прямой путь к достижению Post.
                return actionTargets.First();
            }
            else
            {
                // --- Промежуточная цель, если прямое достижение Post невозможно ---
                // Мы не можем захватить соседа финиша прямо сейчас.
                // Новое Промежуточное Постусловие (Post_intermediate): приблизиться к финишу.
                // Post_intermediate: distance(colony, finishNode) decreases

                // --- Вычисление wp для Post_intermediate ---
                // Чтобы приблизиться к финишу, нужно выбрать клетку на границе, которая ближе всего к нему.
                // wp(Post_intermediate) = frontier.OrderBy(n => distance(n, finishNode))
                var closestToFrontier = frontier
                    .OrderBy(n => CalculateDistance(n, finishNode))
                    .FirstOrDefault();

                return closestToFrontier;
            }
        }

        // --- Фаза 2: Захват оставшейся карты (когда финиш окружен) ---
        /// <summary>
        /// Вычисляет следующий ход после того, как финиш окружен.
        /// </summary>
        public Node CalculateExpansionAfterFinishSurrounded(GameState state)
        {
            var frontier = GetColonyFrontier(state);
            if (!frontier.Any()) return null;

            // Постусловие (Post): Захватить самую "выгодную" клетку.
            // wp(Post) = frontier.OrderByDescending(n => CalculateExpansionProfit(state, n))
            var bestTarget = frontier
                .OrderByDescending(n => CalculateExpansionProfit(state, n))
                .FirstOrDefault();
            return bestTarget;
        }


        // --- Вспомогательные методы ---

        /// <summary>
        /// Проверяет, окружен ли финиш клетками охотника (Post-условие выполнено).
        /// </summary>
        private bool IsFinishSurrounded(GameState state, Node finishNode)
        {
            var finishNeighbors = GetNeighbors(state.CurrentGraph, finishNode);
            return finishNeighbors.All(n => state.HunterControlledNodes.Contains(n.Id));
        }

        /// <summary>
        /// Возвращает "границу" колонии - множество клеток, доступных для захвата.
        /// </summary>
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

        /// <summary>
        /// Вычисляет "выгоду" захвата клетки (используется для wp во второй фазе).
        /// </summary>
        private int CalculateExpansionProfit(GameState state, Node node)
        {
            int score = 100; // Базовая выгода
            if (node.IsVisitedByPlayer) score -= 50;
            var distToPlayer = CalculateDistance(node, state.PlayerPosition);
            score += (int)(50 - distToPlayer);
            return score;
        }

        private double CalculateDistance(Node a, Node b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

        private List<Node> GetNeighbors(Graph graph, Node node)
        {
            return graph.Nodes.Where(n =>
                graph.Edges.Contains((node.Id, n.Id)) || graph.Edges.Contains((n.Id, node.Id))
            ).ToList();
        }
    }
}