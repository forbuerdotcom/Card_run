using Card_run.BattleModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Card_run.Views
{
    public partial class BattleView : UserControl
    {
        public event Action<bool, List<Card>> BattleEnded;

        private List<Card> _playerTeam;
        private List<Card> _enemyTeam;
        private List<Card> _allBattleCards;
        private Card _currentActiveCard;
        private DispatcherTimer _turnTimer;
        private bool _isWaitingForDefenceTarget = false;
        private Card _defendingCard;

        private readonly Random _random = new Random();

        public BattleView()
        {
            InitializeComponent();
        }

        public void StartBattle(List<Card> enemyTeam, List<Card> playerDeckTemplate)
        {
            this.Focus();
            _enemyTeam = enemyTeam;

            _playerTeam = playerDeckTemplate.Select(cardTemplate => new Card(cardTemplate)).ToList();

            _allBattleCards = new List<Card>();
            _allBattleCards.AddRange(_playerTeam);
            _allBattleCards.AddRange(_enemyTeam);

            foreach (var card in _allBattleCards)
            {
                card.TimeToNextTurn = 1000.0 / card.Speed;
            }

            BattleLogPanel.Children.Clear();
            AddLogEntry("Бой начался!");

            InitializeBattle();
        }

        private void InitializeBattle()
        {
            DrawBattlefield();
            CalculateNextTurn();
        }

        private void DrawBattlefield()
        {
            PlayerField.Children.Clear();
            EnemyField.Children.Clear();
            PlayerHand.Children.Clear();

            foreach (var card in _playerTeam)
            {
                var cardControl = new BattleCardControl();
                cardControl.SetCard(card);
                cardControl.CardClicked += OnAnyCardClicked;
                PlayerField.Children.Add(cardControl);
            }

            foreach (var card in _enemyTeam)
            {
                var enemyCardControl = new BattleCardControl();
                enemyCardControl.SetCard(card);
                enemyCardControl.CardClicked += OnAnyCardClicked;
                EnemyField.Children.Add(enemyCardControl);
            }
        }

        private void OnAnyCardClicked(Card clickedCard)
        {
            // 1. Если мы ждем цель для защиты/лечения
            if (_isWaitingForDefenceTarget)
            {
                // Проверяем, что кликнули по союзнику
                if (_playerTeam.Contains(clickedCard))
                {
                    PerformDefence(_defendingCard, clickedCard);
                }
                else
                {
                    MessageBox.Show("Вы можете выбрать только союзника для этого действия!", "Неверная цель");
                }
                return;
            }

            // 2. Стандартная логика хода
            if (_currentActiveCard == null || !_playerTeam.Contains(_currentActiveCard)) return;

            // 3. Игрок кликнул на свою карту -> Начать действие защиты
            if (_playerTeam.Contains(clickedCard))
            {
                _defendingCard = _currentActiveCard; // Запоминаем, кто защищается

                // Проверяем, нужно ли выбирать цель
                if (_defendingCard.DefenceMove == DefenceMove.Shield || _defendingCard.DefenceMove == DefenceMove.Heal)
                {
                    _isWaitingForDefenceTarget = true;
                    MessageBox.Show($"Выберите союзника для действия '{_defendingCard.DefenceMove}'");
                }
                else // SelfShield или SelfHeal
                {
                    PerformDefence(_defendingCard, _defendingCard);
                }
            }
            // 4. Игрок кликнул на врага -> Атака
            else if (_enemyTeam.Contains(clickedCard))
            {
                PerformAttack(_currentActiveCard, clickedCard);
            }
        }

        private void PerformDefence(Card actor, Card target)
        {
            // Проверяем, что target является союзником actor
            if ((_playerTeam.Contains(actor) && !_playerTeam.Contains(target)) ||
                (_enemyTeam.Contains(actor) && !_enemyTeam.Contains(target)))
            {
                // Если target не является союзником, защищаем себя
                target = actor;
            }

            // Сбрасываем состояние ожидания цели
            _isWaitingForDefenceTarget = false;
            _defendingCard = null;

            switch (actor.DefenceMove)
            {
                case DefenceMove.SelfShield:
                case DefenceMove.Shield:
                    target.Shield += 5;
                    AddLogEntry(actor, "создает щит на", target, $"прочность {target.Shield}");
                    break;
                case DefenceMove.SelfHeal:
                case DefenceMove.Heal:
                    int healAmount = (int)(target.MaxHP * 0.2);
                    target.CurrentHP = Math.Min(target.MaxHP, target.CurrentHP + healAmount);
                    AddLogEntry(actor, "исцеляет", target, $"восстановлено {healAmount} здоровья");
                    break;
                case DefenceMove.None:
                default:
                    AddLogEntry(actor, "пропускает ход", null, "");
                    break;
            }
            EndTurn();
        }

        private void PerformAttack(Card attacker, Card defender)
        {
            // Проверка на случай, если по ошибке выбрана уже мертвая цель
            if (defender.Status == CardStatus.Dead)
            {
                AddLogEntry(attacker, "пытается атаковать", defender, "но цель уже мертва!");
                EndTurn();
                return;
            }

            double k = 1.0;
            if (defender.DefenceWeaknesses.Contains(attacker.AttackType)) k = 0.5;
            if (defender.DefenceTypes.Contains(attacker.AttackType)) k = 1.5;

            int damage = (int)Math.Floor((attacker.AD * attacker.Strength) - (defender.Defence * k));
            damage = Math.Max(0, damage);

            // --- Учитываем щит ---
            int shieldDamage = Math.Min(damage, defender.Shield);
            int hpDamage = damage - shieldDamage;

            // --- Создаем единое сообщение о результате атаки ---
            string resultMessage = "";

            if (damage == 0)
            {
                resultMessage = "но атака не наносит урона!";
            }
            else
            {
                if (shieldDamage > 0)
                {
                    defender.Shield -= shieldDamage;
                    resultMessage += $"щит поглощает {shieldDamage} урона. ";
                }

                if (hpDamage > 0)
                {
                    defender.CurrentHP -= hpDamage;
                    resultMessage += $"наносит {hpDamage} урона.";
                }
                else if (shieldDamage > 0) // Случай, когда весь урон поглотил щит
                {
                    resultMessage += "полный урон поглощен щитом.";
                }
                else // Случай, когда урон равен 0 после вычета защиты (но был > 0 до этого)
                {
                    resultMessage = "но атака полностью заблокирована защитой!";
                }
            }

            // Добавляем одну, полную запись в журнал
            AddLogEntry(attacker, "атакует", defender, resultMessage);

            if (defender.CurrentHP <= 0)
            {
                defender.Status = CardStatus.Dead;
                defender.CurrentHP = 0;
                AddLogEntry(defender, "погибает", null, "");
            }

            EndTurn();
        }

        private void EndTurn()
        {
            // Проверяем, уничтожена ли одна из команд
            if (!_playerTeam.Any(c => c.Status == CardStatus.Alive) || !_enemyTeam.Any(c => c.Status == CardStatus.Alive))
            {
                EndBattle();
                return;
            }

            // Если бой продолжается, сбрасываем время для карты, которая только что походила
            if (_currentActiveCard != null)
            {
                _currentActiveCard.TimeToNextTurn = 1000.0 / _currentActiveCard.Speed;
            }

            _currentActiveCard = null;
            UpdateBattlefieldUI();
            CalculateNextTurn();
        }

        private void CalculateNextTurn()
        {
            // 1. Находим карты, которые готовы ходить (время <= 0)
            var readyCards = _allBattleCards.Where(c => c.Status == CardStatus.Alive && c.TimeToNextTurn <= 0).ToList();

            // 2. Если никто не готов, "прокручиваем время" вперед
            if (readyCards.Count == 0)
            {
                var minTime = _allBattleCards.Where(c => c.Status == CardStatus.Alive).Min(c => c.TimeToNextTurn);
                foreach (var card in _allBattleCards)
                {
                    if (card.Status == CardStatus.Alive)
                    {
                        card.TimeToNextTurn -= minTime;
                    }
                }
                // Снова ищем готовые карты
                readyCards = _allBattleCards.Where(c => c.Status == CardStatus.Alive && c.TimeToNextTurn <= 0).ToList();
            }

            // Если и после этого никто не готов (баг), выходим
            if (readyCards.Count == 0) return;

            // 3. Сортируем готовые карты по правилам, которые вы описали
            readyCards = readyCards
                .OrderByDescending(c => c.Speed) // Чем выше скорость, тем раньше
                .ThenBy(c => c.Type == CardType.Aggressive ? 0 : c.Type == CardType.Moderate ? 1 : 2) // Агрессивный > Умеренный > Осторожный
                .ThenByDescending(c => c.Power) // Чем выше сила, тем раньше
                .ThenBy(c => _playerTeam.Contains(c) ? 0 : 1) // Карты игрока ходят раньше врагов при равенстве
                .ToList();

            // 4. Первая карта в отсортированном списке ходит
            _currentActiveCard = readyCards.First();

            if (_currentActiveCard.Shield > 0)
            {
                AddLogEntry(_currentActiveCard, "теряет щит", null, "");
                _currentActiveCard.Shield = 0;
            }

            UpdateTurnOrderUI();
            HighlightActiveCard(_currentActiveCard);

            // Если это карта врага, запускаем ИИ
            if (_enemyTeam.Contains(_currentActiveCard))
            {
                _turnTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
                _turnTimer.Tick += (s, e) =>
                {
                    _turnTimer.Stop();
                    ExecuteEnemyAI();
                };
                _turnTimer.Start();
            }
        }

        private void UpdateBattlefieldUI()
        {
            foreach (BattleCardControl control in PlayerField.Children)
            {
                control.SetCard(control.CardData);
            }
            foreach (BattleCardControl control in EnemyField.Children)
            {
                control.SetCard(control.CardData);
            }
        }

        private void UpdateTurnOrderUI()
        {
            TurnOrderPanel.Children.Clear();
            TurnOrderPanel.Children.Add(new TextBlock
            {
                Text = "Очередь:",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            var sortedCards = _allBattleCards
                .Where(c => c.Status == CardStatus.Alive)
                .OrderBy(c => c.TimeToNextTurn)
                .ToList();

            foreach (var card in sortedCards.Take(10))
            {
                var textBlock = new TextBlock
                {
                    Text = card.Name,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold
                };

                if (_currentActiveCard == card)
                {
                    textBlock.FontSize = 20;
                    textBlock.Foreground = Brushes.Yellow;
                }
                else
                {
                    // Разные цвета для союзников и врагов
                    if (_playerTeam.Contains(card))
                    {
                        textBlock.Foreground = Brushes.LimeGreen;
                    }
                    else
                    {
                        textBlock.Foreground = Brushes.IndianRed;
                    }
                }

                TurnOrderPanel.Children.Add(textBlock);
            }
        }

        private void HighlightActiveCard(Card card)
        {
            foreach (BattleCardControl control in PlayerField.Children)
            {
                control.CardBorder.BorderBrush = Brushes.Black;
                control.CardBorder.BorderThickness = new Thickness(2);
            }
            foreach (BattleCardControl control in EnemyField.Children)
            {
                control.CardBorder.BorderBrush = Brushes.Black;
                control.CardBorder.BorderThickness = new Thickness(2);
            }

            var controlToHighlight = PlayerField.Children.Cast<BattleCardControl>()
                .Concat(EnemyField.Children.Cast<BattleCardControl>())
                .FirstOrDefault(c => c.CardData == card);

            if (controlToHighlight != null)
            {
                controlToHighlight.CardBorder.BorderBrush = Brushes.Yellow;
                controlToHighlight.CardBorder.BorderThickness = new Thickness(4);
            }
        }

        // Проверяет, может ли атакующий убить защищающегося одним ударом
        private bool CanKillCard(Card attacker, Card defender)
        {
            if (defender.Status != CardStatus.Alive) return false;

            double k = 1.0;
            if (defender.DefenceWeaknesses.Contains(attacker.AttackType)) k = 0.5;
            if (defender.DefenceTypes.Contains(attacker.AttackType)) k = 1.5;

            int damage = (int)Math.Floor((attacker.AD * attacker.Strength) - (defender.Defence * k));
            damage = Math.Max(0, damage);
            return damage >= defender.CurrentHP;
        }

        // Находит следующую карту игрока в очереди хода
        private Card GetNextPlayerCard()
        {
            var allAliveCards = _allBattleCards.Where(c => c.Status == CardStatus.Alive).ToList();
            var nextPlayerCard = allAliveCards
                .OrderBy(c => c.TimeToNextTurn)
                .FirstOrDefault(c => _playerTeam.Contains(c));
            return nextPlayerCard;
        }

        // Находит карту игрока с наименьшим здоровьем
        private Card FindPlayerCardWithLowestHP()
        {
            return _playerTeam.Where(c => c.Status == CardStatus.Alive).OrderBy(c => c.CurrentHP).FirstOrDefault();
        }

        private void ExecuteEnemyAI()
        {
            var attacker = _currentActiveCard;
            var alivePlayerCards = _playerTeam.Where(c => c.Status == CardStatus.Alive).ToList();

            if (!alivePlayerCards.Any())
            {
                EndTurn();
                return;
            }

            Card target = null;
            bool shouldDefend = false;

            var nextPlayerCard = GetNextPlayerCard();

            Card FindBestAttackTarget(Card attacker, List<Card> alivePlayerCards)
            {
                if (!alivePlayerCards.Any()) return null;

                var scoredTargets = alivePlayerCards.Select(target =>
                {
                    int score = 0;

                    if (CanKillCard(attacker, target))
                    {
                        score = -1000 - target.AD;
                    }
                    else
                    {
                        score = target.CurrentHP + target.Defence - target.AD;

                        if (target.DefenceWeaknesses.Contains(attacker.AttackType))
                        {
                            score -= 50;
                        }
                    }

                    return new { Target = target, Score = score };
                });

                return scoredTargets.OrderBy(t => t.Score).First().Target;
            }

            switch (attacker.Type)
            {
                case CardType.Aggressive:
                    target = FindBestAttackTarget(attacker, alivePlayerCards);
                    break;

                case CardType.Cautious:
                    bool isInAdvantageousState = false;
                    if (nextPlayerCard != null)
                    {
                        bool hasFullHealth = attacker.CurrentHP == attacker.MaxHP;
                        bool hasDefensiveAdvantage = attacker.DefenceTypes.Contains(nextPlayerCard.AttackType);
                        bool hasNumericalAdvantage = _enemyTeam.Count(c => c.Status == CardStatus.Alive) > alivePlayerCards.Count;

                        isInAdvantageousState = hasFullHealth && hasDefensiveAdvantage && hasNumericalAdvantage;
                    }

                    if (!isInAdvantageousState)
                    {
                        var allyInDanger = FindAllyInDangerOfBeingKilled(nextPlayerCard);
                        if (allyInDanger != null && CanSupportAllies(attacker))
                        {
                            target = allyInDanger;
                            shouldDefend = true;
                        }
                        else if (FindAllyWithLowHealth() != null && CanSupportAllies(attacker))
                        {
                            target = FindAllyWithLowHealth();
                            shouldDefend = true;
                        }
                        if (nextPlayerCard != null && nextPlayerCard.AD >= attacker.CurrentHP + attacker.Defence && attacker.DefenceMove != DefenceMove.None)
                            shouldDefend = true;
                        else if (nextPlayerCard != null && nextPlayerCard.DefenceWeaknesses.Contains(attacker.AttackType) && attacker.CurrentHP <= attacker.MaxHP * 0.3 && attacker.DefenceMove != DefenceMove.None)
                            shouldDefend = true;
                        else if (attacker.DefenceMove == DefenceMove.Heal && attacker.CurrentHP <= attacker.MaxHP * 0.5)
                            shouldDefend = true;
                    }
                    if (!shouldDefend)
                    {
                        target = FindBestAttackTarget(attacker, alivePlayerCards);
                    }
                    break;

                case CardType.Moderate:
                    var allyInDangerMod = FindAllyInDangerOfBeingKilled(nextPlayerCard);
                    if (allyInDangerMod != null && CanSupportAllies(attacker))
                    {
                        target = allyInDangerMod;
                        shouldDefend = true;
                    }
                    if (nextPlayerCard != null && nextPlayerCard.AD >= attacker.CurrentHP + attacker.Defence && attacker.DefenceMove != DefenceMove.None)
                        shouldDefend = true;
                    else if (nextPlayerCard != null && nextPlayerCard.AD >= nextPlayerCard.CurrentHP + nextPlayerCard.Defence && alivePlayerCards.Count > _enemyTeam.Count(c => c.Status == CardStatus.Alive) && attacker.DefenceMove != DefenceMove.None)
                        shouldDefend = true;
                    else if (nextPlayerCard != null && nextPlayerCard.AD >= nextPlayerCard.CurrentHP + nextPlayerCard.Defence && alivePlayerCards.Count <= _enemyTeam.Count(c => c.Status == CardStatus.Alive) && alivePlayerCards.Any(c => CanKillCard(attacker, c)))
                        target = FindPlayerCardWithLowestHP();
                    else if (attacker.DefenceMove == DefenceMove.Heal && attacker.CurrentHP <= attacker.MaxHP * 0.65 && nextPlayerCard != null && nextPlayerCard.DefenceWeaknesses.Contains(attacker.AttackType))
                        shouldDefend = true;
                    else if (attacker.DefenceMove == DefenceMove.Heal && attacker.CurrentHP <= attacker.MaxHP * 0.4)
                        shouldDefend = true;
                    else if (nextPlayerCard != null && nextPlayerCard.DefenceWeaknesses.Contains(attacker.AttackType) && attacker.CurrentHP <= attacker.MaxHP * 0.5 && attacker.DefenceMove != DefenceMove.None)
                        shouldDefend = true;
                    else if (!shouldDefend)
                    {
                        target = FindBestAttackTarget(attacker, alivePlayerCards);
                    }
                    break;
            }

            // Выполняем действие
            if (shouldDefend)
            {
                PerformDefence(attacker, attacker);
            }
            else if (target != null)
            {
                PerformAttack(attacker, target);
            }
            else
            {
                EndTurn();
            }
        }

        private void AddLogEntry(string systemMessage)
        {
            var logText = new TextBlock
            {
                Text = systemMessage,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5, 2, 5, 2),
                Foreground = Brushes.Yellow,
                FontWeight = FontWeights.Bold
            };

            BattleLogPanel.Children.Add(logText);
            var scrollViewer = (BattleLogPanel.Parent as ScrollViewer);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToBottom();
            }
        }

        private void AddLogEntry(Card actorCard, string action, Card targetCard, string result)
        {
            var logText = new TextBlock
            {
                Text = $"\"{actorCard.Name}\" {action} \"{(targetCard?.Name ?? "самого себя")}\" и {result}.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5, 2, 5, 2)
            };

            // Определяем цвет по принадлежности к команде
            if (_playerTeam.Contains(actorCard))
            {
                logText.Foreground = Brushes.LimeGreen;
            }
            else
            {
                logText.Foreground = Brushes.IndianRed;
            }

            // Добавляем запись в панель
            BattleLogPanel.Children.Add(logText);

            // Автоматически прокручиваем к новой записи
            var scrollViewer = (BattleLogPanel.Parent as ScrollViewer);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToBottom();
            }
        }

        // Находит союзника оппонента, который может быть убит следующей картой игрока
        private Card FindAllyInDangerOfBeingKilled(Card nextPlayerCard)
        {
            if (nextPlayerCard == null) return null;
            return _enemyTeam.Where(c => c.Status == CardStatus.Alive && CanKillCard(nextPlayerCard, c)).FirstOrDefault();
        }

        // Находит союзника оппонента с здоровьем ниже 50%
        private Card FindAllyWithLowHealth()
        {
            return _enemyTeam.Where(c => c.Status == CardStatus.Alive && c.CurrentHP <= c.MaxHP * 0.5).FirstOrDefault();
        }

        // Проверяет, может ли карта лечить или накладывать щит на союзников
        private bool CanSupportAllies(Card card)
        {
            return card.DefenceMove == DefenceMove.Shield || card.DefenceMove == DefenceMove.Heal;
        }

        private void EndBattle()
        {
            _turnTimer?.Stop();
            bool playerWon = _playerTeam.Any(c => c.Status == CardStatus.Alive);

            // Находим всех побежденных врагов в этом бою
            var defeatedEnemies = _enemyTeam.Where(c => c.Status == CardStatus.Dead).ToList();

            BattleEnded?.Invoke(playerWon, defeatedEnemies);
        }

        private void ExitBattle_Click(object sender, RoutedEventArgs e)
        {
            // При выходе из боя считаем это поражением и передаем пустой список побежденных врагов.
            BattleEnded?.Invoke(false, new List<Card>());
        }
    }
}