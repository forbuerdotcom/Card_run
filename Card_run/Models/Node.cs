namespace Card_run.Models
{
    public class Node
    {
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public bool IsPlayerStart { get; set; }
        public bool IsFinish { get; set; }
        public bool IsHunter { get; set; }
        public bool IsPlayerCurrentPosition { get; set; }

        // НОВОЕ: Свойство для магазина
        public bool IsShop { get; set; }

        /// <summary>
        /// Посещал ли игрок эту клетку.
        /// </summary>
        public bool IsVisitedByPlayer { get; set; }

        /// <summary>
        /// Посещал ли игрок эту клетку охотника (фиолетовую зону).
        /// </summary>
        public bool IsVisitedByPlayerHunter { get; set; }

        public bool IsBattleNode { get; set; }
        public BattleDifficulty BattleDifficulty { get; set; }
        public List<int> EnemyTeamIds { get; set; } // ID врагов для этого узла
        public bool IsCleared { get; set; } // Очищен ли боевой узел (враги побеждены)
    }

    public enum BattleDifficulty
    {
        Weak,   // Зеленый
        Medium, // Красный
        Strong  // Черный
    }
}