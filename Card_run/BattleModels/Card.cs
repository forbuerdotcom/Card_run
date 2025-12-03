using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Card_run.BattleModels
{
    // Реализуем интерфейс INotifyPropertyChanged
    public class Card : INotifyPropertyChanged
    {
        // Событие, которое WPF слушает для обновления UI
        public event PropertyChangedEventHandler PropertyChanged;

        // Вспомогательный метод для вызова события
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // --- Свойства ---

        private int _currentHP;
        public int CurrentHP
        {
            get => _currentHP;
            set
            {
                if (_currentHP != value)
                {
                    _currentHP = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _shield;
        public int Shield
        {
            get => _shield;
            set
            {
                if (_shield != value)
                {
                    _shield = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _shieldValue;
        public int ShieldValue
        {
            get => _shieldValue;
            set
            {
                if (_shieldValue != value)
                {
                    _shieldValue = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _healValue;
        public int HealValue
        {
            get => _healValue;
            set
            {
                if (_healValue != value)
                {
                    _healValue = value;
                    OnPropertyChanged();
                }
            }
        }

        private CardStatus _status;
        public CardStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        // Остальные свойства
        public string Name { get; set; }
        public int MaxHP { get; set; }
        public int Speed { get; set; }
        public int Defence { get; set; }
        public List<DamageType> DefenceTypes { get; set; }
        public List<DamageType> DefenceWeaknesses { get; set; }
        public int AD { get; set; }
        public DamageType AttackType { get; set; }
        public int Strength { get; set; }
        public DefenceMove DefenceMove { get; set; }
        public string PerkName { get; set; }
        public CardType Type { get; set; }
        public int Power { get; set; }
        public string ImagePath { get; set; }
        public double TimeToNextTurn { get; set; }

        // --- Конструкторы ---

        public Card()
        {
            DefenceTypes = new List<DamageType>();
            DefenceWeaknesses = new List<DamageType>();
            Status = CardStatus.Alive;
            Shield = 0;
            ShieldValue = 5;
            HealValue = 7;
        }

        public Card(Card other) : this()
        {
            Name = other.Name;
            MaxHP = other.MaxHP;
            CurrentHP = other.MaxHP;
            Speed = other.Speed;
            Defence = other.Defence;
            DefenceTypes = new List<DamageType>(other.DefenceTypes);
            DefenceWeaknesses = new List<DamageType>(other.DefenceWeaknesses);
            AD = other.AD;
            AttackType = other.AttackType;
            Strength = other.Strength;
            DefenceMove = other.DefenceMove;
            PerkName = other.PerkName;
            Type = other.Type;
            Power = other.Power;
            ImagePath = other.ImagePath;
            Shield = other.Shield;
            ShieldValue = other.ShieldValue;
            HealValue = other.HealValue;
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CardStatus
    {
        Alive,
        Dead,
        Stunned,
        Stasis
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CardType
    {
        Aggressive,
        Moderate,
        Cautious
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DefenceMove
    {
        Heal,
        SelfHeal,
        Shield,
        SelfShield,
        Stasis,
        None
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DamageType
    {
        Physical,
        Magical,
        Fire,
        Water,
        Air,
        Earth,
        Lightning,
        Light,
        Dark
    }

}
