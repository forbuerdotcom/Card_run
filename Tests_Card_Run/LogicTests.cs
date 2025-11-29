// LogicTests.cs
using Card_run.BattleModels;
using Card_run.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xunit;

public class LogicTests
{
    #region Card Tests
    public class CardTests
    {
        // Пояснение: убеждаемся, что копирующий конструктор создает глубокую копию карты для безопасных манипуляций в логике.
        [Fact]
        public void Card_CopyConstructor_ShouldCreateDeepCopy()
        {
            var originalCard = new Card
            {
                Name = "Тестовый маг",
                MaxHP = 30,
                CurrentHP = 25,
                AD = 10,
                Defence = 5,
                Power = 7
            };

            var copiedCard = new Card(originalCard);

            Assert.NotSame(originalCard, copiedCard);
            Assert.Equal(originalCard.Name, copiedCard.Name);
            Assert.Equal(originalCard.MaxHP, copiedCard.MaxHP);
            Assert.Equal(originalCard.CurrentHP, copiedCard.CurrentHP);
            Assert.Equal(originalCard.AD, copiedCard.AD);
            Assert.Equal(originalCard.Defence, copiedCard.Defence);
            Assert.Equal(originalCard.Power, copiedCard.Power);

            copiedCard.CurrentHP = 10;
            Assert.NotEqual(originalCard.CurrentHP, copiedCard.CurrentHP);
        }

        // Пояснение: проверяем, что изменение здоровья корректно уведомляет UI через INotifyPropertyChanged.
        [Fact]
        public void Card_CurrentHPChange_ShouldRaisePropertyChangedEvent()
        {
            var card = new Card { CurrentHP = 20 };
            PropertyChangedEventArgs eventArgs = null;
            card.PropertyChanged += (_, args) => eventArgs = args;

            card.CurrentHP = 15;

            Assert.NotNull(eventArgs);
            Assert.Equal(nameof(Card.CurrentHP), eventArgs.PropertyName);
        }

        // Пояснение: удостоверяемся, что изменение щита тоже вызывает событие и UI перерисует значение.
        [Fact]
        public void Card_ShieldChange_ShouldRaisePropertyChangedEvent()
        {
            var card = new Card { Shield = 0 };
            PropertyChangedEventArgs eventArgs = null;
            card.PropertyChanged += (_, args) => eventArgs = args;

            card.Shield = 3;

            Assert.NotNull(eventArgs);
            Assert.Equal(nameof(Card.Shield), eventArgs.PropertyName);
        }

        // Пояснение: негативный сценарий — повторное задание того же статуса не должно триггерить обновление UI.
        [Fact]
        public void Card_SettingSameStatus_ShouldNotRaiseEvent()
        {
            var card = new Card { Status = CardStatus.Alive };
            bool eventRaised = false;
            card.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(Card.Status))
                {
                    eventRaised = true;
                }
            };

            card.Status = CardStatus.Alive;

            Assert.False(eventRaised);
        }
    }
    #endregion

    #region GameState Tests
    public class GameStateTests
    {
        private static Graph CreateSimpleGraph()
        {
            var graph = new Graph();
            var nodes = new List<Node>
            {
                new Node { Id = 0, X = 0, Y = 0, IsPlayerStart = true },
                new Node { Id = 1, X = 100, Y = 0, IsHunter = true },
                new Node { Id = 2, X = 200, Y = 0, IsFinish = true }
            };
            graph.Nodes = nodes;
            graph.Edges = new List<(int, int)> { (0, 1), (1, 2) };
            return graph;
        }

        // Пояснение: базовый сценарий перемещения по допустимому ребру должен менять позицию и счётчики.
        [Fact]
        public void MovePlayer_ToValidNeighbor_ShouldSucceedAndUpdatePosition()
        {
            var graph = CreateSimpleGraph();
            var gameState = new GameState(graph);
            var destinationNode = graph.Nodes.First(n => n.Id == 1);

            bool result = gameState.MovePlayer(destinationNode);

            Assert.True(result);
            Assert.Equal(destinationNode, gameState.PlayerPosition);
            Assert.Equal(1, gameState.PlayerMoveCount);
            Assert.True(destinationNode.IsVisitedByPlayer);
        }

        // Пояснение: негативный сценарий — попытка прыжка через несоседний узел должна завершиться провалом.
        [Fact]
        public void MovePlayer_ToInvalidNode_ShouldFailAndNotChangePosition()
        {
            var graph = CreateSimpleGraph();
            var gameState = new GameState(graph);
            var invalidNode = graph.Nodes.First(n => n.Id == 2);

            bool result = gameState.MovePlayer(invalidNode);

            Assert.False(result);
            Assert.NotEqual(invalidNode, gameState.PlayerPosition);
            Assert.Equal(0, gameState.PlayerMoveCount);
        }

        // Пояснение: расширение колонии должно помечать узел как контролируемый охотником.
        [Fact]
        public void ExpandHunterColony_ShouldAddNodeToControlledSet()
        {
            var graph = CreateSimpleGraph();
            var gameState = new GameState(graph);
            var nodeToInfect = graph.Nodes.First(n => n.Id == 2);

            gameState.ExpandHunterColony(nodeToInfect);

            Assert.Contains(nodeToInfect.Id, gameState.HunterControlledNodes);
        }

        // Пояснение: доступные ходы должны совпадать со списком соседей стартовой вершины.
        [Fact]
        public void GetAvailableMoves_ShouldReturnCorrectNeighbors()
        {
            var graph = CreateSimpleGraph();
            var gameState = new GameState(graph);

            var availableMoves = gameState.GetAvailableMoves();

            Assert.Single(availableMoves);
            Assert.Contains(graph.Nodes.First(n => n.Id == 1), availableMoves);
        }

        // Пояснение: перемещение в клетку охотника должно отмечать её как посещённую в заражённой зоне.
        [Fact]
        public void MovePlayer_IntoHunterNode_ShouldMarkHunterVisit()
        {
            var graph = new Graph();
            var start = new Node { Id = 0, IsPlayerStart = true };
            var hunter = new Node { Id = 1, IsHunter = true };
            graph.Nodes = new List<Node> { start, hunter };
            graph.Edges = new List<(int, int)> { (0, 1) };
            var gameState = new GameState(graph);

            bool moved = gameState.MovePlayer(hunter);

            Assert.True(moved);
            Assert.True(hunter.IsVisitedByPlayerHunter);
            Assert.Equal(hunter, gameState.PlayerPosition);
        }

        // Пояснение: негативный случай — если у узла нет соседей, список доступных ходов должен быть пуст.
        [Fact]
        public void GetAvailableMoves_NoEdges_ShouldReturnEmptyList()
        {
            var graph = new Graph();
            var start = new Node { Id = 0, IsPlayerStart = true };
            graph.Nodes = new List<Node> { start };
            graph.Edges = new List<(int, int)>();
            var gameState = new GameState(graph);

            var moves = gameState.GetAvailableMoves();

            Assert.Empty(moves);
        }
    }

    public class GameStateNegativeTests
    {
        private static Graph CreateSimpleGraph()
        {
            var graph = new Graph();
            var nodes = new List<Node>
            {
                new Node { Id = 0, X = 0, Y = 0, IsPlayerStart = true },
                new Node { Id = 1, X = 100, Y = 0, IsHunter = true },
                new Node { Id = 2, X = 200, Y = 0, IsFinish = true }
            };
            graph.Nodes = nodes;
            graph.Edges = new List<(int, int)> { (0, 1), (1, 2) };
            return graph;
        }

        // Пояснение: без стартовой клетки игрока состояние игры не может быть создано.
        [Fact]
        public void Constructor_WithGraphWithoutPlayerStart_ShouldThrowException()
        {
            var graph = new Graph();
            graph.Nodes = new List<Node> { new Node { Id = 0, IsFinish = true } };

            Assert.Throws<InvalidOperationException>(() => new GameState(graph));
        }

        // Пояснение: передача null как цели хода должна приводить к ArgumentNullException.
        [Fact]
        public void MovePlayer_WithNullDestination_ShouldThrowArgumentNullException()
        {
            var graph = CreateSimpleGraph();
            var gameState = new GameState(graph);

            Assert.Throws<ArgumentNullException>(() => gameState.MovePlayer(null));
        }

        // Пояснение: повторное заражение уже контролируемого узла не должно менять состояние.
        [Fact]
        public void ExpandHunterColony_ToAlreadyControlledNode_ShouldNotChangeState()
        {
            var graph = CreateSimpleGraph();
            var gameState = new GameState(graph);
            var nodeToInfect = graph.Nodes.First(n => n.IsHunter);
            int initialCount = gameState.HunterControlledNodes.Count;

            gameState.ExpandHunterColony(nodeToInfect);

            Assert.Equal(initialCount, gameState.HunterControlledNodes.Count);
        }
    }
    #endregion

    #region HunterAI Tests
    public class HunterAITests
    {
        private static Graph CreateTestGraph()
        {
            var graph = new Graph();
            var nodes = new List<Node>
            {
                new Node { Id = 0, X = 0, Y = 0, IsPlayerStart = true },
                new Node { Id = 1, X = 100, Y = 0, IsHunter = true },
                new Node { Id = 2, X = 200, Y = 0 },
                new Node { Id = 3, X = 150, Y = 100, IsFinish = true },
                new Node { Id = 4, X = 50, Y = 100, IsShop = true }
            };
            graph.Nodes = nodes;
            graph.Edges = new List<(int, int)> { (0, 1), (1, 2), (2, 3), (1, 4) };
            return graph;
        }

        private static void PerformMoves(GameState state, params int[] nodeIds)
        {
            foreach (var id in nodeIds)
            {
                var destination = state.CurrentGraph.Nodes.First(n => n.Id == id);
                state.MovePlayer(destination);
            }
        }

        // Пояснение: пока игрок сделал меньше трёх ходов, охотник не расширяется.
        [Fact]
        public void CalculateNextExpansion_PlayerMoveCountLessThan3_ShouldReturnNull()
        {
            var graph = CreateTestGraph();
            var gameState = new GameState(graph);
            PerformMoves(gameState, 1, 0);
            var hunterAI = new HunterAI();

            var nextNode = hunterAI.CalculateNextExpansion(gameState);

            Assert.Null(nextNode);
        }

        // Пояснение: если финиш не окружён, ИИ должен выбирать соседние к нему клетки.
        [Fact]
        public void CalculateNextExpansion_FinishNotSurrounded_ShouldPrioritizeFinishNeighbor()
        {
            var graph = CreateTestGraph();
            var gameState = new GameState(graph);
            PerformMoves(gameState, 1, 0, 1);
            var hunterAI = new HunterAI();
            gameState.ExpandHunterColony(graph.Nodes.First(n => n.Id == 1));

            var nextNode = hunterAI.CalculateNextExpansion(gameState);

            Assert.NotNull(nextNode);
            Assert.Equal(2, nextNode.Id);
        }

        // Пояснение: охотник не должен заражать магазины, даже если они на границе.
        [Fact]
        public void CalculateNextExpansion_ShouldNotPickShopNode()
        {
            var graph = CreateTestGraph();
            var gameState = new GameState(graph);
            PerformMoves(gameState, 1, 0, 1);
            var hunterAI = new HunterAI();

            var nextNode = hunterAI.CalculateNextExpansion(gameState);

            Assert.NotNull(nextNode);
            Assert.NotEqual(4, nextNode.Id);
        }

        // Пояснение: негативный сценарий — при отсутствии доступных соседей расширение возвращает null.
        [Fact]
        public void CalculateNextExpansion_EmptyFrontier_ShouldReturnNull()
        {
            var graph = new Graph();
            var start = new Node { Id = 0, IsPlayerStart = true };
            var hunter = new Node { Id = 1, IsHunter = true };
            var finish = new Node { Id = 2, IsFinish = true };
            var helperA = new Node { Id = 3 };
            var helperB = new Node { Id = 4 };
            graph.Nodes = new List<Node> { start, hunter, finish, helperA, helperB };
            graph.Edges = new List<(int, int)> { (0, 3), (3, 4) };
            var gameState = new GameState(graph);
            PerformMoves(gameState, 3, 4, 3, 0, 3);
            var hunterAI = new HunterAI();

            var nextNode = hunterAI.CalculateNextExpansion(gameState);

            Assert.Null(nextNode);
        }
    }
    #endregion

    #region GraphGenerator Tests
    public class GraphGeneratorTests
    {
        // Пояснение: проверяем, что генератор создаёт связный граф с ключевыми типами узлов.
        [Fact]
        public void Generate_ShouldCreateValidGraph()
        {
            var generator = new GraphGenerator();

            var graph = generator.Generate();

            Assert.NotNull(graph);
            Assert.InRange(graph.Nodes.Count, 25, 35);
            Assert.NotNull(graph.Nodes.FirstOrDefault(n => n.IsPlayerStart));
            Assert.NotNull(graph.Nodes.FirstOrDefault(n => n.IsFinish));
            Assert.NotNull(graph.Nodes.FirstOrDefault(n => n.IsHunter));
            Assert.NotNull(graph.Nodes.FirstOrDefault(n => n.IsShop));

            var startNode = graph.Nodes.First(n => n.IsPlayerStart);
            var visitedNodes = new HashSet<int> { startNode.Id };
            var queue = new Queue<Node>(new[] { startNode });

            while (queue.Count > 0)
            {
                var currentNode = queue.Dequeue();
                var neighbors = graph.Nodes.Where(n =>
                    graph.Edges.Contains((currentNode.Id, n.Id)) ||
                    graph.Edges.Contains((n.Id, currentNode.Id))
                ).ToList();

                foreach (var neighbor in neighbors)
                {
                    if (visitedNodes.Add(neighbor.Id))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
            Assert.Equal(graph.Nodes.Count, visitedNodes.Count);
        }
    }
    #endregion

    #region Preparation Logic Tests
    public class PreparationLogicTests
    {
        // Пояснение: пустая колода формально валидна и не вызывает ошибок интерфейса.
        [Fact]
        public void IsDeckValid_EmptyDeck_ShouldReturnTrue()
        {
            var deck = new List<Card>();

            var isValid = IsDeckValid(deck, out _);

            Assert.True(isValid);
        }

        // Пояснение: набор негативных сценариев — каждый доводит колоду до одного из ограничений.
        [Theory]
        [InlineData(6, 0, 0, 0, "В колоде не может быть более 5 карт.")]
        [InlineData(5, 2, 0, 0, "В колоде не может быть более одной карты с силой 9-10.")]
        [InlineData(5, 1, 4, 0, "В колоде не может быть более трёх карт с силой 7-8.")]
        [InlineData(5, 1, 3, 36, "Суммарный показатель силы колоды не может превышать 35.")]
        public void IsDeckValid_InvalidDeck_ShouldReturnFalseWithCorrectMessage(int cardCount, int highPowerCount, int midPowerCount, int totalPower, string expectedError)
        {
            var deck = new List<Card>();
            for (int i = 0; i < cardCount; i++)
            {
                int power = 1;
                if (i < highPowerCount) power = 9;
                else if (i < highPowerCount + midPowerCount) power = 7;
                deck.Add(new Card { Name = $"Card {i}", Power = power });
            }
            if (totalPower > 0 && deck.Sum(c => c.Power) != totalPower)
            {
                deck.Last().Power = totalPower - deck.Take(deck.Count - 1).Sum(c => c.Power);
            }

            var isValid = IsDeckValid(deck, out string errorMessage);

            Assert.False(isValid);
            Assert.Equal(expectedError, errorMessage);
        }

        private bool IsDeckValid(List<Card> deck, out string errorMessage)
        {
            errorMessage = null;
            if (deck.Count > 5) { errorMessage = "В колоде не может быть более 5 карт."; return false; }
            if (deck.Count(c => c.Power >= 9) > 1) { errorMessage = "В колоде не может быть более одной карты с силой 9-10."; return false; }
            if (deck.Count(c => c.Power >= 7 && c.Power <= 8) > 3) { errorMessage = "В колоде не может быть более трёх карт с силой 7-8."; return false; }
            if (deck.Sum(c => c.Power) > 35) { errorMessage = "Суммарный показатель силы колоды не может превышать 35."; return false; }
            return true;
        }
    }
    #endregion
}
