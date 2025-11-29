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
    /// <summary>
    /// Представление боевой системы игры.
    /// Управляет пошаговым боем между картами игрока и врагов.
    /// Использует систему очереди ходов на основе скорости карт.
    /// </summary>
    public partial class BattleView : UserControl
    {
        // Событие, которое вызывается при окончании боя
        // Первый параметр - победил ли игрок, второй - список побежденных врагов
        public event Action<bool, List<Card>> BattleEnded;

        // Команда карт игрока на поле боя
        private List<Card> _playerTeam;
        
        // Команда карт врагов на поле боя
        private List<Card> _enemyTeam;
        
        // Все карты на поле боя (игрок + враги) для управления очередью ходов
        private List<Card> _allBattleCards;
        
        // Карта, которая сейчас должна ходить
        private Card _currentActiveCard;
        
        // Таймер для задержки хода врага (для визуального эффекта)
        private DispatcherTimer _turnTimer;
        
        // Флаг, указывающий, что мы ждем выбора цели для защитного действия
        private bool _isWaitingForDefenceTarget = false;
        
        // Карта, которая выполняет защитное действие и ждет выбора цели
        private Card _defendingCard;

        // Генератор случайных чисел для различных вычислений
        private readonly Random _random = new Random();

        /// <summary>
        /// Конструктор представления боя.
        /// Инициализирует компоненты интерфейса.
        /// </summary>
        public BattleView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Начинает новый бой.
        /// Инициализирует команды, рассчитывает время до хода для всех карт и начинает первый ход.
        /// </summary>
        /// <param name="enemyTeam">Команда врагов</param>
        /// <param name="playerDeckTemplate">Шаблон колоды игрока (создаются копии карт)</param>
        public void StartBattle(List<Card> enemyTeam, List<Card> playerDeckTemplate)
        {
            // Устанавливаем фокус на представление для обработки ввода
            this.Focus();
            
            // Сохраняем команду врагов
            _enemyTeam = enemyTeam;

            // Создаем копии карт игрока из шаблона (чтобы не изменять оригиналы)
            _playerTeam = playerDeckTemplate.Select(cardTemplate => new Card(cardTemplate)).ToList();

            // Объединяем все карты в один список для управления очередью
            _allBattleCards = new List<Card>();
            _allBattleCards.AddRange(_playerTeam);
            _allBattleCards.AddRange(_enemyTeam);

            // Рассчитываем время до следующего хода для каждой карты
            // Формула: 1000 миллисекунд / скорость карты
            // Чем выше скорость, тем меньше время до хода
            foreach (var card in _allBattleCards)
            {
                card.TimeToNextTurn = 1000.0 / card.Speed;
            }

            // Очищаем журнал боя и добавляем начальное сообщение
            BattleLogPanel.Children.Clear();
            AddLogEntry("Бой начался!");

            // Инициализируем бой (рисуем поле и рассчитываем первый ход)
            InitializeBattle();
        }

        /// <summary>
        /// Инициализирует бой: рисует поле боя и рассчитывает первый ход.
        /// </summary>
        private void InitializeBattle()
        {
            // Рисуем карты на поле боя
            DrawBattlefield();
            // Рассчитываем, какая карта ходит первой
            CalculateNextTurn();
        }

        /// <summary>
        /// Рисует поле боя: размещает карты игрока и врагов на соответствующих панелях.
        /// </summary>
        private void DrawBattlefield()
        {
            // Очищаем все панели от предыдущих элементов
            PlayerField.Children.Clear();
            EnemyField.Children.Clear();
            PlayerHand.Children.Clear();

            // Создаем элементы управления для каждой карты игрока
            foreach (var card in _playerTeam)
            {
                var cardControl = new BattleCardControl();
                cardControl.SetCard(card);
                // Подписываемся на событие клика по карте
                cardControl.CardClicked += OnAnyCardClicked;
                // Добавляем карту на поле игрока
                PlayerField.Children.Add(cardControl);
            }

            // Создаем элементы управления для каждой карты врага
            foreach (var card in _enemyTeam)
            {
                var enemyCardControl = new BattleCardControl();
                enemyCardControl.SetCard(card);
                // Подписываемся на событие клика по карте
                enemyCardControl.CardClicked += OnAnyCardClicked;
                // Добавляем карту на поле врага
                EnemyField.Children.Add(enemyCardControl);
            }
        }

        /// <summary>
        /// Обработчик клика по любой карте на поле боя.
        /// В зависимости от состояния и выбранной карты выполняет атаку или защитное действие.
        /// </summary>
        private void OnAnyCardClicked(Card clickedCard)
        {
            // --- СЛУЧАЙ 1: Ожидание выбора цели для защитного действия ---
            if (_isWaitingForDefenceTarget)
            {
                // Проверяем, что кликнули по союзнику (карте игрока)
                if (_playerTeam.Contains(clickedCard))
                {
                    // Выполняем защитное действие на выбранную цель
                    PerformDefence(_defendingCard, clickedCard);
                }
                else
                {
                    // Если кликнули не по союзнику, показываем ошибку
                    MessageBox.Show("Вы можете выбрать только союзника для этого действия!", "Неверная цель");
                }
                return;
            }

            // --- СЛУЧАЙ 2: Стандартная логика хода ---
            // Проверяем, что сейчас ход карты игрока
            if (_currentActiveCard == null || !_playerTeam.Contains(_currentActiveCard)) return;

            // --- СЛУЧАЙ 3: Игрок кликнул на свою карту -> Начать действие защиты ---
            if (_playerTeam.Contains(clickedCard))
            {
                // Запоминаем, какая карта будет защищаться
                _defendingCard = _currentActiveCard;

                // Проверяем, нужно ли выбирать цель для защитного действия
                // Shield и Heal требуют выбора союзника
                if (_defendingCard.DefenceMove == DefenceMove.Shield || _defendingCard.DefenceMove == DefenceMove.Heal)
                {
                    // Включаем режим ожидания выбора цели
                    _isWaitingForDefenceTarget = true;
                    MessageBox.Show($"Выберите союзника для действия '{_defendingCard.DefenceMove}'");
                }
                else // SelfShield или SelfHeal применяются к себе автоматически
                {
                    // Выполняем защитное действие на себя
                    PerformDefence(_defendingCard, _defendingCard);
                }
            }
            // --- СЛУЧАЙ 4: Игрок кликнул на врага -> Атака ---
            else if (_enemyTeam.Contains(clickedCard))
            {
                // Выполняем атаку текущей активной карты на выбранного врага
                PerformAttack(_currentActiveCard, clickedCard);
            }
        }

        /// <summary>
        /// Выполняет защитное действие карты.
        /// В зависимости от типа действия создает щит, лечит или применяет другие эффекты.
        /// </summary>
        /// <param name="actor">Карта, которая выполняет действие</param>
        /// <param name="target">Цель действия (может быть сама карта или союзник)</param>
        private void PerformDefence(Card actor, Card target)
        {
            // Сбрасываем состояние ожидания цели
            _isWaitingForDefenceTarget = false;
            _defendingCard = null;

            // Выполняем действие в зависимости от типа защитного действия
            switch (actor.DefenceMove)
            {
                case DefenceMove.SelfShield:
                    // SelfShield всегда применяется к себе
                    // Получаем значение щита из характеристики карты
                    int selfShieldValue = actor.ShieldValue;
                    // Добавляем щит к текущему значению
                    actor.Shield += selfShieldValue;
                    // Записываем действие в журнал
                    AddLogEntry(actor, "создает щит на", actor, $"прочность {actor.Shield}");
                    break;

                case DefenceMove.Shield:
                    // Shield может применяться к союзнику
                    Card shieldTarget = target;
                    // Проверяем, что цель является союзником
                    if ((_playerTeam.Contains(actor) && !_playerTeam.Contains(target)) ||
                        (_enemyTeam.Contains(actor) && !_enemyTeam.Contains(target)))
                    {
                        // Если цель не является союзником, защищаем себя
                        shieldTarget = actor;
                    }
                    // Получаем значение щита и применяем к цели
                    int shieldValue = actor.ShieldValue;
                    shieldTarget.Shield += shieldValue;
                    AddLogEntry(actor, "создает щит на", shieldTarget, $"прочность {shieldTarget.Shield}");
                    break;

                case DefenceMove.SelfHeal:
                    // SelfHeal всегда применяется к себе, только если карта жива
                    if (actor.Status == CardStatus.Alive)
                    {
                        // Получаем значение лечения
                        int selfHealAmount = actor.HealValue;
                        // Восстанавливаем здоровье, но не больше максимального
                        actor.CurrentHP = Math.Min(actor.MaxHP, actor.CurrentHP + selfHealAmount);
                        AddLogEntry(actor, "исцеляет", actor, $"восстановлено {selfHealAmount} здоровья");
                    }
                    else
                    {
                        // Если карта мертва, лечение невозможно
                        AddLogEntry(actor, "не может исцелить", actor, "цель мертва");
                    }
                    break;

                case DefenceMove.Heal:
                    // Heal может применяться к союзнику
                    Card healTarget = target;
                    // Проверяем, что цель является союзником
                    if ((_playerTeam.Contains(actor) && !_playerTeam.Contains(target)) ||
                        (_enemyTeam.Contains(actor) && !_enemyTeam.Contains(target)))
                    {
                        // Если цель не является союзником, лечим себя
                        healTarget = actor;
                    }
                    // Проверяем, что цель лечения жива
                    if (healTarget.Status == CardStatus.Alive)
                    {
                        // Получаем значение лечения и применяем к цели
                        int healAmount = actor.HealValue;
                        // Восстанавливаем здоровье, но не больше максимального
                        healTarget.CurrentHP = Math.Min(healTarget.MaxHP, healTarget.CurrentHP + healAmount);
                        AddLogEntry(actor, "исцеляет", healTarget, $"восстановлено {healAmount} здоровья");
                    }
                    else
                    {
                        // Если цель мертва, лечение невозможно
                        AddLogEntry(actor, "не может исцелить", healTarget, "цель мертва");
                    }
                    break;

                case DefenceMove.None:
                default:
                    // Если у карты нет защитного действия, она пропускает ход
                    AddLogEntry(actor, "пропускает ход");
                    break;
            }
            // Завершаем ход после выполнения действия
            EndTurn();
        }

        /// <summary>
        /// Выполняет атаку одной карты на другую.
        /// Рассчитывает урон с учетом защиты, щитов, типов урона и уязвимостей.
        /// </summary>
        /// <param name="attacker">Карта, которая атакует</param>
        /// <param name="defender">Карта, которая защищается</param>
        private void PerformAttack(Card attacker, Card defender)
        {
            // Проверка на случай, если по ошибке выбрана уже мертвая цель
            if (defender.Status == CardStatus.Dead)
            {
                AddLogEntry(attacker, "пытается атаковать", defender, "но цель уже мертва!");
                EndTurn();
                return;
            }

            // --- РАСЧЕТ КОЭФФИЦИЕНТА УРОНА ---
            // Базовый коэффициент урона
            double k = 1.0;
            // Если у защитника есть уязвимость к типу атаки атакующего, урон уменьшается в 2 раза
            if (defender.DefenceWeaknesses.Contains(attacker.AttackType)) k = 0.5;
            // Если у защитника есть защита от типа атаки атакующего, урон увеличивается в 1.5 раза
            if (defender.DefenceTypes.Contains(attacker.AttackType)) k = 1.5;

            // --- РАСЧЕТ УРОНА ---
            // Формула: (Атака * Сила) - (Защита * Коэффициент)
            // Урон не может быть отрицательным
            int damage = (int)Math.Floor((attacker.AD * attacker.Strength) - (defender.Defence * k));
            damage = Math.Max(0, damage);

            // --- УЧЕТ ЩИТА ---
            // Сначала урон поглощается щитом
            int shieldDamage = Math.Min(damage, defender.Shield);
            // Оставшийся урон идет на здоровье
            int hpDamage = damage - shieldDamage;

            // --- СОЗДАНИЕ СООБЩЕНИЯ О РЕЗУЛЬТАТЕ АТАКИ ---
            string resultMessage = "";

            if (damage == 0)
            {
                // Если урон равен 0, атака не наносит урона
                resultMessage = "но атака не наносит урона!";
            }
            else
            {
                // Если щит поглотил часть урона
                if (shieldDamage > 0)
                {
                    // Уменьшаем щит на поглощенный урон
                    defender.Shield -= shieldDamage;
                    resultMessage += $"щит поглощает {shieldDamage} урона. ";
                }

                // Если остался урон для здоровья
                if (hpDamage > 0)
                {
                    // Уменьшаем здоровье на оставшийся урон
                    defender.CurrentHP -= hpDamage;
                    resultMessage += $"наносит {hpDamage} урона.";
                }
                else if (shieldDamage > 0) // Случай, когда весь урон поглотил щит
                {
                    resultMessage += "полный урон поглощен щитом.";
                }
                else // Случай, когда урон равен 0 после вычета защиты
                {
                    resultMessage = "но атака полностью заблокирована защитой!";
                }
            }

            // Добавляем запись в журнал боя
            AddLogEntry(attacker, "атакует", defender, resultMessage);

            // Проверяем, не погибла ли карта после атаки
            if (defender.CurrentHP <= 0)
            {
                // Помечаем карту как мертвую
                defender.Status = CardStatus.Dead;
                defender.CurrentHP = 0;
                AddLogEntry(defender, "погибает");
            }

            // Завершаем ход после атаки
            EndTurn();
        }

        /// <summary>
        /// Завершает текущий ход и переходит к следующему.
        /// Проверяет условия окончания боя и обновляет интерфейс.
        /// </summary>
        private void EndTurn()
        {
            // Проверяем, уничтожена ли одна из команд
            // Если у игрока или врагов не осталось живых карт, бой заканчивается
            if (!_playerTeam.Any(c => c.Status == CardStatus.Alive) || !_enemyTeam.Any(c => c.Status == CardStatus.Alive))
            {
                EndBattle();
                return;
            }

            // Если бой продолжается, сбрасываем время для карты, которая только что походила
            if (_currentActiveCard != null)
            {
                // Рассчитываем время до следующего хода: 1000 мс / скорость карты
                _currentActiveCard.TimeToNextTurn = 1000.0 / _currentActiveCard.Speed;
            }

            // Сбрасываем текущую активную карту
            _currentActiveCard = null;
            // Обновляем отображение поля боя
            UpdateBattlefieldUI();
            // Рассчитываем следующую карту, которая должна ходить
            CalculateNextTurn();
        }

        /// <summary>
        /// Рассчитывает, какая карта должна ходить следующей.
        /// Использует систему очереди на основе скорости карт и их характеристик.
        /// </summary>
        private void CalculateNextTurn()
        {
            // --- ЭТАП 1: НАХОДИМ КАРТЫ, КОТОРЫЕ ГОТОВЫ ХОДИТЬ ---
            // Карта готова ходить, если её время до хода <= 0
            var readyCards = _allBattleCards.Where(c => c.Status == CardStatus.Alive && c.TimeToNextTurn <= 0).ToList();

            // --- ЭТАП 2: ЕСЛИ НИКТО НЕ ГОТОВ, ПРОКРУЧИВАЕМ ВРЕМЯ ---
            if (readyCards.Count == 0)
            {
                // Находим минимальное время до хода среди всех живых карт
                var minTime = _allBattleCards.Where(c => c.Status == CardStatus.Alive).Min(c => c.TimeToNextTurn);
                // Вычитаем это время из времени всех карт (прокручиваем время вперед)
                foreach (var card in _allBattleCards)
                {
                    if (card.Status == CardStatus.Alive)
                    {
                        card.TimeToNextTurn -= minTime;
                    }
                }
                // Снова ищем готовые карты после прокрутки времени
                readyCards = _allBattleCards.Where(c => c.Status == CardStatus.Alive && c.TimeToNextTurn <= 0).ToList();
            }

            // Если и после этого никто не готов (не должно происходить), выходим
            if (readyCards.Count == 0) return;

            // --- ЭТАП 3: СОРТИРУЕМ ГОТОВЫЕ КАРТЫ ПО ПРИОРИТЕТУ ---
            readyCards = readyCards
                // Приоритет 1: Чем выше скорость, тем раньше ходит
                .OrderByDescending(c => c.Speed)
                // Приоритет 2: Тип карты (Агрессивный > Умеренный > Осторожный)
                .ThenBy(c => c.Type == CardType.Aggressive ? 0 : c.Type == CardType.Moderate ? 1 : 2)
                // Приоритет 3: Чем выше сила, тем раньше ходит
                .ThenByDescending(c => c.Power)
                // Приоритет 4: Карты игрока ходят раньше врагов при равенстве всех параметров
                .ThenBy(c => _playerTeam.Contains(c) ? 0 : 1)
                .ToList();

            // --- ЭТАП 4: ПЕРВАЯ КАРТА В ОТСОРТИРОВАННОМ СПИСКЕ ХОДИТ ---
            _currentActiveCard = readyCards.First();

            // Если у активной карты есть щит, он теряется в начале хода
            if (_currentActiveCard.Shield > 0)
            {
                AddLogEntry(_currentActiveCard, "теряет щит", null, "");
                _currentActiveCard.Shield = 0;
            }

            // Обновляем отображение очереди ходов
            UpdateTurnOrderUI();
            // Подсвечиваем активную карту на поле боя
            HighlightActiveCard(_currentActiveCard);

            // Если это карта врага, запускаем ИИ с задержкой для визуального эффекта
            if (_enemyTeam.Contains(_currentActiveCard))
            {
                // Создаем таймер с задержкой 1.5 секунды
                _turnTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
                _turnTimer.Tick += (s, e) =>
                {
                    // Останавливаем таймер и выполняем ход врага
                    _turnTimer.Stop();
                    ExecuteEnemyAI();
                };
                _turnTimer.Start();
            }
        }

        /// <summary>
        /// Обновляет отображение карт на поле боя.
        /// Вызывается после каждого хода для отображения изменений в характеристиках.
        /// </summary>
        private void UpdateBattlefieldUI()
        {
            // Обновляем все карты игрока
            foreach (BattleCardControl control in PlayerField.Children)
            {
                control.SetCard(control.CardData);
            }
            // Обновляем все карты врагов
            foreach (BattleCardControl control in EnemyField.Children)
            {
                control.SetCard(control.CardData);
            }
        }

        /// <summary>
        /// Обновляет отображение очереди ходов в левой панели.
        /// Показывает следующие 10 карт, которые будут ходить, в порядке очереди.
        /// </summary>
        private void UpdateTurnOrderUI()
        {
            // Очищаем панель очереди
            TurnOrderPanel.Children.Clear();
            
            // Добавляем заголовок
            TurnOrderPanel.Children.Add(new TextBlock
            {
                Text = "Очередь:",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // Сортируем все живые карты по времени до хода (кто быстрее ходит)
            var sortedCards = _allBattleCards
                .Where(c => c.Status == CardStatus.Alive)
                .OrderBy(c => c.TimeToNextTurn)
                .ToList();

            // Отображаем первые 10 карт из очереди
            foreach (var card in sortedCards.Take(10))
            {
                var textBlock = new TextBlock
                {
                    Text = card.Name,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold
                };

                // Если это текущая активная карта, выделяем её
                if (_currentActiveCard == card)
                {
                    textBlock.FontSize = 20;
                    textBlock.Foreground = Brushes.Yellow; // Желтый цвет для активной карты
                }
                else
                {
                    // Разные цвета для союзников и врагов
                    if (_playerTeam.Contains(card))
                    {
                        textBlock.Foreground = Brushes.LimeGreen; // Зеленый для карт игрока
                    }
                    else
                    {
                        textBlock.Foreground = Brushes.IndianRed; // Красный для врагов
                    }
                }

                TurnOrderPanel.Children.Add(textBlock);
            }
        }

        /// <summary>
        /// Подсвечивает активную карту на поле боя желтой рамкой.
        /// Сбрасывает подсветку со всех остальных карт.
        /// </summary>
        private void HighlightActiveCard(Card card)
        {
            // Сбрасываем подсветку со всех карт игрока
            foreach (BattleCardControl control in PlayerField.Children)
            {
                control.CardBorder.BorderBrush = Brushes.Black;
                control.CardBorder.BorderThickness = new Thickness(2);
            }
            // Сбрасываем подсветку со всех карт врагов
            foreach (BattleCardControl control in EnemyField.Children)
            {
                control.CardBorder.BorderBrush = Brushes.Black;
                control.CardBorder.BorderThickness = new Thickness(2);
            }

            // Находим элемент управления для активной карты
            var controlToHighlight = PlayerField.Children.Cast<BattleCardControl>()
                .Concat(EnemyField.Children.Cast<BattleCardControl>())
                .FirstOrDefault(c => c.CardData == card);

            // Если карта найдена, подсвечиваем её желтой рамкой
            if (controlToHighlight != null)
            {
                controlToHighlight.CardBorder.BorderBrush = Brushes.Yellow;
                controlToHighlight.CardBorder.BorderThickness = new Thickness(4);
            }
        }

        /// <summary>
        /// Проверяет, может ли атакующий убить защищающегося одним ударом.
        /// Используется ИИ для принятия решений.
        /// </summary>
        private bool CanKillCard(Card attacker, Card defender)
        {
            // Если защитник уже мертв, убить его нельзя
            if (defender.Status != CardStatus.Alive) return false;

            // Рассчитываем коэффициент урона (аналогично PerformAttack)
            double k = 1.0;
            if (defender.DefenceWeaknesses.Contains(attacker.AttackType)) k = 0.5;
            if (defender.DefenceTypes.Contains(attacker.AttackType)) k = 1.5;

            // Рассчитываем урон
            int damage = (int)Math.Floor((attacker.AD * attacker.Strength) - (defender.Defence * k));
            damage = Math.Max(0, damage);
            
            // Проверяем, достаточно ли урона для убийства (с учетом щита)
            return damage >= defender.CurrentHP + defender.Shield;
        }

        /// <summary>
        /// Находит следующую карту игрока в очереди хода.
        /// Используется ИИ для оценки угрозы.
        /// </summary>
        private Card GetNextPlayerCard()
        {
            // Получаем все живые карты
            var allAliveCards = _allBattleCards.Where(c => c.Status == CardStatus.Alive).ToList();
            // Находим первую карту игрока в отсортированной по времени очереди
            var nextPlayerCard = allAliveCards
                .OrderBy(c => c.TimeToNextTurn)
                .FirstOrDefault(c => _playerTeam.Contains(c));
            return nextPlayerCard;
        }

        /// <summary>
        /// Находит карту игрока с наименьшим здоровьем.
        /// Используется ИИ для выбора цели атаки.
        /// </summary>
        private Card FindPlayerCardWithLowestHP()
        {
            return _playerTeam.Where(c => c.Status == CardStatus.Alive).OrderBy(c => c.CurrentHP).FirstOrDefault();
        }

        /// <summary>
        /// Выполняет ход врага с использованием ИИ.
        /// ИИ анализирует ситуацию и выбирает оптимальное действие в зависимости от типа карты.
        /// </summary>
        private void ExecuteEnemyAI()
        {
            // Получаем карту, которая сейчас ходит (враг)
            var attacker = _currentActiveCard;
            // Получаем список всех живых карт игрока
            var alivePlayerCards = _playerTeam.Where(c => c.Status == CardStatus.Alive).ToList();

            // Если у игрока не осталось живых карт, завершаем ход
            if (!alivePlayerCards.Any())
            {
                EndTurn();
                return;
            }

            // Переменные для хранения выбранной цели и решения о защите
            Card target = null;
            bool shouldDefend = false;

            // Получаем следующую карту игрока в очереди для оценки угрозы
            var nextPlayerCard = GetNextPlayerCard();

            /// <summary>
            /// Внутренняя функция для поиска лучшей цели для атаки.
            /// Оценивает каждую цель по системе очков и выбирает наиболее выгодную.
            /// </summary>
            Card FindBestAttackTarget(Card attacker, List<Card> alivePlayerCards)
            {
                if (!alivePlayerCards.Any()) return null;

                // Оцениваем каждую цель по системе очков
                var scoredTargets = alivePlayerCards.Select(target =>
                {
                    int score = 0;

                    // Если можно убить цель одним ударом, это очень приоритетная цель
                    if (CanKillCard(attacker, target))
                    {
                        // Чем больше урон у цели, тем выше приоритет (меньше очков = выше приоритет)
                        score = -1000 - target.AD;
                    }
                    else
                    {
                        // Обычная оценка: здоровье + защита - урон цели
                        score = target.CurrentHP + target.Defence - target.AD;

                        // Если у цели есть уязвимость к типу атаки, снижаем очки (повышаем приоритет)
                        if (target.DefenceWeaknesses.Contains(attacker.AttackType))
                        {
                            score -= 50;
                        }
                    }

                    return new { Target = target, Score = score };
                });

                // Возвращаем цель с наименьшими очками (наибольшим приоритетом)
                return scoredTargets.OrderBy(t => t.Score).First().Target;
            }

            // Выбираем действие в зависимости от типа карты врага
            switch (attacker.Type)
            {
                case CardType.Aggressive:
                    // Агрессивные карты всегда атакуют лучшую цель
                    target = FindBestAttackTarget(attacker, alivePlayerCards);
                    break;

                case CardType.Cautious:
                    // Осторожные карты анализируют ситуацию перед действием
                    bool isInAdvantageousState = false;
                    if (nextPlayerCard != null)
                    {
                        // Проверяем, находится ли карта в выгодном положении
                        bool hasFullHealth = attacker.CurrentHP == attacker.MaxHP;
                        bool hasDefensiveAdvantage = attacker.DefenceTypes.Contains(nextPlayerCard.AttackType);
                        bool hasNumericalAdvantage = _enemyTeam.Count(c => c.Status == CardStatus.Alive) > alivePlayerCards.Count;

                        // Если все условия выполнены, карта в выгодном положении
                        isInAdvantageousState = hasFullHealth && hasDefensiveAdvantage && hasNumericalAdvantage;
                    }

                    // Если не в выгодном положении, проверяем необходимость защиты
                    if (!isInAdvantageousState)
                    {
                        // Проверяем, есть ли союзник в опасности
                        var allyInDanger = FindAllyInDangerOfBeingKilled(nextPlayerCard);
                        if (allyInDanger != null && CanSupportAllies(attacker))
                        {
                            // Защищаем союзника
                            target = allyInDanger;
                            shouldDefend = true;
                        }
                        // Проверяем, есть ли союзник с низким здоровьем
                        else if (FindAllyWithLowHealth() != null && CanSupportAllies(attacker))
                        {
                            target = FindAllyWithLowHealth();
                            shouldDefend = true;
                        }
                        // Проверяем, может ли следующая карта игрока убить эту карту
                        if (nextPlayerCard != null && nextPlayerCard.AD >= attacker.CurrentHP + attacker.Defence && attacker.DefenceMove != DefenceMove.None)
                            shouldDefend = true;
                        // Проверяем другие условия для защиты
                        else if (nextPlayerCard != null && nextPlayerCard.DefenceWeaknesses.Contains(attacker.AttackType) && attacker.CurrentHP <= attacker.MaxHP * 0.3 && attacker.DefenceMove != DefenceMove.None)
                            shouldDefend = true;
                        // Если здоровье ниже 50% и есть лечение, лечимся
                        else if (attacker.DefenceMove == DefenceMove.Heal && attacker.CurrentHP <= attacker.MaxHP * 0.5)
                            shouldDefend = true;
                    }
                    // Если не нужно защищаться, атакуем
                    if (!shouldDefend)
                    {
                        target = FindBestAttackTarget(attacker, alivePlayerCards);
                    }
                    break;

                case CardType.Moderate:
                    // Умеренные карты балансируют между атакой и защитой
                    var allyInDangerMod = FindAllyInDangerOfBeingKilled(nextPlayerCard);
                    // Если союзник в опасности и можем помочь, помогаем
                    if (allyInDangerMod != null && CanSupportAllies(attacker))
                    {
                        target = allyInDangerMod;
                        shouldDefend = true;
                    }
                    // Проверяем различные условия для защиты
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
                    // Если не нужно защищаться, атакуем
                    else if (!shouldDefend)
                    {
                        target = FindBestAttackTarget(attacker, alivePlayerCards);
                    }
                    break;
            }

            // Выполняем выбранное действие
            if (shouldDefend)
            {
                // Выполняем защитное действие на себя
                PerformDefence(attacker, attacker);
            }
            else if (target != null)
            {
                // Атакуем выбранную цель
                PerformAttack(attacker, target);
            }
            else
            {
                // Если не выбрано действие, завершаем ход
                EndTurn();
            }
        }

        /// <summary>
        /// Добавляет системное сообщение в журнал боя.
        /// Используется для важных событий боя (начало, конец и т.д.).
        /// </summary>
        private void AddLogEntry(string systemMessage)
        {
            var logText = new TextBlock
            {
                Text = systemMessage,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5, 2, 5, 2),
                Foreground = Brushes.Yellow, // Желтый цвет для системных сообщений
                FontWeight = FontWeights.Bold
            };

            BattleLogPanel.Children.Add(logText);
            // Автоматически прокручиваем журнал к новой записи
            var scrollViewer = (BattleLogPanel.Parent as ScrollViewer);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToBottom();
            }
        }

        /// <summary>
        /// Добавляет запись о действии карты в журнал боя.
        /// Используется для действий без цели (например, "пропускает ход").
        /// </summary>
        private void AddLogEntry(Card actorCard, string action)
        {
            var logText = new TextBlock
            {
                Text = $"\"{actorCard.Name}\" {action}.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5, 2, 5, 2)
            };

            // Определяем цвет по принадлежности к команде
            if (_playerTeam.Contains(actorCard))
            {
                logText.Foreground = Brushes.LimeGreen; // Зеленый для карт игрока
            }
            else
            {
                logText.Foreground = Brushes.IndianRed; // Красный для врагов
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

        /// <summary>
        /// Добавляет запись о действии карты с целью в журнал боя.
        /// Используется для атак, лечения, создания щитов и т.д.
        /// </summary>
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
                logText.Foreground = Brushes.LimeGreen; // Зеленый для карт игрока
            }
            else
            {
                logText.Foreground = Brushes.IndianRed; // Красный для врагов
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

        /// <summary>
        /// Находит союзника врага, который может быть убит следующей картой игрока.
        /// Используется ИИ для принятия решений о защите союзников.
        /// </summary>
        private Card FindAllyInDangerOfBeingKilled(Card nextPlayerCard)
        {
            if (nextPlayerCard == null) return null;
            // Ищем живых союзников, которых может убить следующая карта игрока
            return _enemyTeam.Where(c => c.Status == CardStatus.Alive && CanKillCard(nextPlayerCard, c)).FirstOrDefault();
        }

        /// <summary>
        /// Находит союзника врага с здоровьем ниже 50%.
        /// Используется ИИ для выбора цели для лечения.
        /// </summary>
        private Card FindAllyWithLowHealth()
        {
            return _enemyTeam.Where(c => c.Status == CardStatus.Alive && c.CurrentHP <= c.MaxHP * 0.5).FirstOrDefault();
        }

        /// <summary>
        /// Проверяет, может ли карта лечить или накладывать щит на союзников.
        /// Используется ИИ для определения возможности поддержки союзников.
        /// </summary>
        private bool CanSupportAllies(Card card)
        {
            return card.DefenceMove == DefenceMove.Shield || card.DefenceMove == DefenceMove.Heal;
        }

        /// <summary>
        /// Завершает бой и вызывает событие окончания боя.
        /// Определяет победителя и передает список побежденных врагов.
        /// </summary>
        private void EndBattle()
        {
            // Останавливаем таймер, если он был запущен
            _turnTimer?.Stop();
            
            // Определяем победителя: если у игрока осталась хотя бы одна живая карта, он победил
            bool playerWon = _playerTeam.Any(c => c.Status == CardStatus.Alive);

            // Находим всех побежденных врагов в этом бою
            var defeatedEnemies = _enemyTeam.Where(c => c.Status == CardStatus.Dead).ToList();

            // Вызываем событие окончания боя с результатом и списком побежденных врагов
            BattleEnded?.Invoke(playerWon, defeatedEnemies);
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Выйти из боя".
        /// При выходе бой считается проигранным.
        /// </summary>
        private void ExitBattle_Click(object sender, RoutedEventArgs e)
        {
            // При выходе из боя считаем это поражением и передаем пустой список побежденных врагов
            BattleEnded?.Invoke(false, new List<Card>());
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Инструкция".
        /// Показывает модальное окно с подробной инструкцией по использованию боевой системы.
        /// </summary>
        private void ShowInstruction_Click(object sender, RoutedEventArgs e)
        {
            string instruction = @"ИНСТРУКЦИЯ ПО БОЕВОЙ СИСТЕМЕ

1. ОБЩАЯ ИНФОРМАЦИЯ:
   - Бой происходит пошагово, карты ходят по очереди
   - Очередь хода определяется скоростью карт
   - Ваша цель - уничтожить всех врагов, сохранив хотя бы одну свою карту
   - Если все ваши карты погибнут, вы проиграете бой

2. СИСТЕМА ОЧЕРЕДИ ХОДОВ:
   - Карты ходят в порядке очереди, которая отображается слева
   - Очередь определяется скоростью карты: чем выше скорость, тем чаще ход
   - При равенстве скорости приоритет: Агрессивный > Умеренный > Осторожный
   - При полном равенстве карты игрока ходят раньше врагов
   - Текущая активная карта подсвечена желтой рамкой

3. КАК ХОДИТЬ:
   - Когда ходит ваша карта (подсвечена желтой рамкой), вы можете выбрать действие
   - Кликните на врага, чтобы атаковать его
   - Кликните на свою карту, чтобы выполнить защитное действие
   - Если у карты есть защитное действие, требующее выбора цели, выберите союзника

4. АТАКА:
   - Кликните на врага, когда ходит ваша карта
   - Урон рассчитывается: (Атака * Сила) - (Защита * Коэффициент)
   - Если у врага есть уязвимость к типу вашей атаки, урон уменьшается в 2 раза
   - Если у врага есть защита от типа вашей атаки, урон увеличивается в 1.5 раза
   - Сначала урон поглощается щитом, затем идет на здоровье
   - Если здоровье карты падает до 0, она погибает

5. ЗАЩИТНЫЕ ДЕЙСТВИЯ:
   - SelfShield: создает щит на себя автоматически
   - Shield: создает щит на союзника (нужно выбрать цель)
   - SelfHeal: восстанавливает здоровье себе автоматически
   - Heal: восстанавливает здоровье союзнику (нужно выбрать цель)
   - None: карта не имеет защитного действия

6. ЩИТЫ:
   - Щит поглощает урон перед здоровьем
   - Щит теряется в начале каждого хода карты
   - Щит можно накапливать, применяя защитные действия несколько раз

7. ТИПЫ УРОНА И ЗАЩИТЫ:
   - Каждая карта имеет тип атаки (Physical, Magical, Fire, Water и т.д.)
   - Карты могут иметь защиту от определенных типов урона
   - Карты могут иметь уязвимости к определенным типам урона
   - Используйте это для эффективных атак и защиты

8. ИИ ВРАГОВ:
   - Враги управляются искусственным интеллектом
   - Агрессивные враги всегда атакуют
   - Осторожные враги анализируют ситуацию и могут защищаться
   - Умеренные враги балансируют между атакой и защитой
   - Враги выбирают оптимальные цели для атаки

9. ЖУРНАЛ БОЯ:
   - Справа отображается журнал всех действий в бою
   - Зеленым цветом отображаются действия ваших карт
   - Красным цветом отображаются действия врагов
   - Желтым цветом отображаются системные сообщения
   - Журнал автоматически прокручивается к новым записям

10. СТРАТЕГИЯ:
    - Уничтожайте слабых врагов первыми, чтобы уменьшить их количество
    - Используйте защитные действия для поддержки союзников
    - Обращайте внимание на типы урона и защиты
    - Используйте щиты для защиты от сильных атак
    - Лечите союзников с низким здоровьем

11. ВЫХОД ИЗ БОЯ:
    - Кнопка 'Выйти из боя' позволяет покинуть бой
    - Выход из боя считается поражением
    - Вы не получите награды за побежденных врагов

12. ПОБЕДА И ПОРАЖЕНИЕ:
    - Вы побеждаете, если уничтожите всех врагов
    - Вы проигрываете, если все ваши карты погибнут
    - За каждого побежденного врага вы получаете золото";

            MessageBox.Show(instruction, "Инструкция по бою", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
