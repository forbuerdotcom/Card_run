// Путь: Models/GraphGenerator.cs
using Card_run.BattleModels;
using System.Collections.Generic;
using System.Linq;
using static Card_run.Models.Node;

namespace Card_run.Models
{
    public class GraphGenerator
    {
        private Random _random = new Random();
        private List<Card> _allEnemyCards;

        public GraphGenerator()
        {
            // Загружаем все возможные карты врагов один раз при создании генератора
            _allEnemyCards = DataLoader.GetAllCards();
        }

        public Graph Generate()
        {
            Graph graph;
            Node playerStart, hunterStart, finish, shop;

            // Генерируем граф в цикле, пока не получим валидный
            do
            {
                graph = GenerateRawGraph();
                playerStart = graph.Nodes.First(n => n.IsPlayerStart);
                hunterStart = graph.Nodes.FirstOrDefault(n => n.IsHunter);
                finish = graph.Nodes.First(n => n.IsFinish);
                shop = graph.Nodes.FirstOrDefault(n => n.IsShop);

            } while (!IsGraphValid(graph, playerStart, hunterStart, finish, shop));

            return graph;
        }

        // НОВЫЙ МЕТОД: Проверяет, есть ли путь от игрока к финишу, минуя охотника и магазин
        private bool IsGraphValid(Graph graph, Node playerStart, Node hunterStart, Node finish, Node shop)
        {
            var obstacles = new HashSet<int>();
            if (hunterStart != null) obstacles.Add(hunterStart.Id);
            if (shop != null) obstacles.Add(shop.Id);

            // Если нет препятствий, граф валиден
            if (!obstacles.Any()) return true;

            // Используем BFS, чтобы найти путь, считая клетки охотника и магазина непроходимыми
            var path = FindShortestPath(graph, playerStart, finish, obstacles);
            return path != null;
        }

        // Старый метод генерации, переименованный
        private Graph GenerateRawGraph()
        {
            var graph = new Graph();
            int cellSize = 80;
            int gridWidth = 20;
            int gridHeight = 10;
            
            var potentialNodes = new List<Node>();
            for (int x = 1; x < gridWidth; x += 2)
            {
                for (int y = 1; y < gridHeight; y += 2)
                {
                    potentialNodes.Add(new Node { Id = potentialNodes.Count, X = x * cellSize, Y = y * cellSize });
                }
            }

            int nodeCount = _random.Next(25, 35);
            var selectedNodes = potentialNodes.OrderBy(x => _random.Next()).Take(nodeCount).ToList();
            graph.Nodes = selectedNodes;

            foreach (var node in graph.Nodes)
            {
                var nearestNeighbors = graph.Nodes
                    .Where(n => n.Id != node.Id)
                    .OrderBy(n => Distance(node, n))
                    .Take(2 + _random.Next(0, 2));

                foreach (var neighbor in nearestNeighbors)
                {
                    if (!graph.Edges.Contains((node.Id, neighbor.Id)) && !graph.Edges.Contains((neighbor.Id, node.Id)))
                    {
                        graph.Edges.Add((node.Id, neighbor.Id));
                    }
                }
            }
            EnsureConnectivity(graph);
            PlaceGameObjects(graph);
            return graph;
        }

        private void PlaceGameObjects(Graph graph)
        {
            if (graph.Nodes.Count < 5) return;

            var playerStart = graph.Nodes.OrderByDescending(n => n.X).First();
            playerStart.IsPlayerStart = true;

            var finish = graph.Nodes.OrderByDescending(n => Distance(playerStart, n)).First();
            finish.IsFinish = true;

            // Охотник ставится в соседнюю к игроку клетку, они не спавнятся вместе
            var playerNeighbors = graph.Nodes
                .Where(n => graph.Edges.Contains((playerStart.Id, n.Id)) || graph.Edges.Contains((n.Id, playerStart.Id)))
                .ToList();

            if (playerNeighbors.Any())
            {
                var hunterNode = playerNeighbors.OrderBy(x => _random.Next()).First();
                hunterNode.IsHunter = true;
            }

            // Магазин ставится в случайную свободную клетку
            var availableNodesForShop = graph.Nodes
                .Where(n => !n.IsPlayerStart && !n.IsFinish && !n.IsHunter)
                .ToList();

            if (availableNodesForShop.Any())
            {
                var shopNode = availableNodesForShop.OrderBy(x => _random.Next()).First();
                shopNode.IsShop = true;
            }

            var availableNodesForBattle = graph.Nodes
                .Where(n => !n.IsPlayerStart && !n.IsFinish && !n.IsHunter && !n.IsShop)
                .ToList();

            if (!availableNodesForBattle.Any()) return;

            int maxStrongNodes = 2;
            int currentStrongNodes = 0;
            int maxMediumNodes = 10;
            int currentMediumNodes = 0;

            foreach (var node in availableNodesForBattle)
            {
                node.IsBattleNode = true;

                // Генерируем ID врагов для этого узла
                node.EnemyTeamIds = GenerateEnemyTeamIds();

                // Рассчитываем сложность на основе ID
                var enemyTeamForDifficulty = node.EnemyTeamIds.Select(id => _allEnemyCards[id]).ToList();
                double difficulty = (enemyTeamForDifficulty.Sum(c => c.Power) - enemyTeamForDifficulty.Count) / (double)enemyTeamForDifficulty.Count;

                if (difficulty >= 8 && currentStrongNodes < maxStrongNodes)
                {
                    node.BattleDifficulty = BattleDifficulty.Strong;
                    currentStrongNodes++;
                }
                else if (difficulty >= 5 && currentMediumNodes < maxMediumNodes)
                {
                    node.BattleDifficulty = BattleDifficulty.Medium;
                    currentMediumNodes++;
                }
                else
                {
                    node.BattleDifficulty = BattleDifficulty.Weak;
                }
            }
        }

        private List<int> GenerateEnemyTeamIds()
        {
            int teamSize = _random.Next(1, 6); // от 1 до 5 врагов
            var enemyTeamIds = new List<int>();
            int cardCount = _allEnemyCards.Count;

            for (int i = 0; i < teamSize; i++)
            {
                // Получаем случайный индекс и добавляем его в список
                int randomIndex = _random.Next(cardCount);
                enemyTeamIds.Add(randomIndex);
            }
            return enemyTeamIds;
        }

        // --- Вспомогательные методы ---
        private double Distance(Node a, Node b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

        private void EnsureConnectivity(Graph graph)
        {
            if (graph.Nodes.Count < 2) return;
            var connected = new HashSet<int> { graph.Nodes.First().Id };
            var toConnect = graph.Nodes.Where(n => !connected.Contains(n.Id)).ToList();
            while (toConnect.Any())
            {
                var edgeToAdd = (
                    from connectedNode in graph.Nodes.Where(n => connected.Contains(n.Id))
                    from disconnectedNode in toConnect
                    orderby Distance(connectedNode, disconnectedNode)
                    select (From: connectedNode.Id, To: disconnectedNode.Id)
                ).First();
                graph.Edges.Add((edgeToAdd.From, edgeToAdd.To));
                connected.Add(edgeToAdd.To);
                toConnect.Remove(graph.Nodes.First(n => n.Id == edgeToAdd.To));
            }
        }

        private List<Node> FindShortestPath(Graph graph, Node start, Node end, ISet<int> obstacles)
        {
            var queue = new Queue<List<Node>>();
            var visited = new HashSet<int>();
            queue.Enqueue(new List<Node> { start });
            visited.Add(start.Id);

            while (queue.Count > 0)
            {
                var path = queue.Dequeue();
                var currentNode = path.Last();

                if (currentNode.Id == end.Id) return path;

                foreach (var neighbor in GetNeighbors(graph, currentNode))
                {
                    if (!visited.Contains(neighbor.Id) && !obstacles.Contains(neighbor.Id))
                    {
                        visited.Add(neighbor.Id);
                        var newPath = new List<Node>(path) { neighbor };
                        queue.Enqueue(newPath);
                    }
                }
            }
            return null;
        }

        private List<Node> GetNeighbors(Graph graph, Node node)
        {
            return graph.Nodes.Where(n =>
                graph.Edges.Contains((node.Id, n.Id)) || graph.Edges.Contains((n.Id, node.Id))
            ).ToList();
        }
    }
}