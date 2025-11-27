using System.Collections.Generic;

namespace Card_run.Models
{
    /// <summary>
    /// Представляет граф, состоящий из вершин и ребер.
    /// </summary>
    public class Graph
    {
        /// <summary>
        /// Список всех вершин в графе.
        /// </summary>
        public List<Node> Nodes { get; set; }

        /// <summary>
        /// Список всех ребер. Каждое ребро - это пара ID вершин (откуда и куда).
        /// </summary>
        public List<(int From, int To)> Edges { get; set; }

        /// <summary>
        /// Конструктор, который инициализирует списки, чтобы избежать ошибок.
        /// </summary>
        public Graph()
        {
            Nodes = new List<Node>();
            Edges = new List<(int, int)>();
        }
    }
}