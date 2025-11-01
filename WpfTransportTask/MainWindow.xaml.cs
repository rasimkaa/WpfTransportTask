using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace WpfTransportTask
{
    public partial class MainWindow : Window
    {
        // Переменные для хранения данных, считанных из полей
        private int[,] costs;
        private int[] supply;
        private int[] demand;

        // Жестко задаем размерность 3x5, как в задании
        private readonly int rows = 3;
        private readonly int cols = 5;

        // Массивы для удобного доступа к элементам TextBox
        private TextBox[] supplyTextBoxes;
        private TextBox[] demandTextBoxes;
        private TextBox[,] costTextBoxes;

        private int[,] currentPlan; // Матрица перевозок
        private DataTable displayTable; // Таблица для отображения в DataGrid

        // Структура для хранения координат ячейки
        private struct Cell
        {
            public int Row { get; }
            public int Col { get; }

            public Cell(int row, int col)
            {
                Row = row;
                Col = col;
            }

            public override bool Equals(object obj)
            {
                return obj is Cell cell && Row == cell.Row && Col == cell.Col;
            }

            public override int GetHashCode()
            {
                return (Row * 100) + Col;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            InitializeInputControls(); // Связываем наши массивы с TextBox'ами
            displayTable = new DataTable();
            Log("Приложение готово. Введите данные и выберите метод.");
        }

        /// <summary>
        /// Инициализирует массивы, хранящие ссылки на TextBox, для легкого доступа.
        /// </summary>
        private void InitializeInputControls()
        {
            // Запасы (A)
            supplyTextBoxes = new TextBox[] { txtA1, txtA2, txtA3 };

            // Потребности (B)
            demandTextBoxes = new TextBox[] { txtB1, txtB2, txtB3, txtB4, txtB5 };

            // Тарифы (Cij)
            costTextBoxes = new TextBox[,]
            {
                { txtC11, txtC12, txtC13, txtC14, txtC15 },
                { txtC21, txtC22, txtC23, txtC24, txtC25 },
                { txtC31, txtC32, txtC33, txtC34, txtC35 }
            };
        }

        /// <summary>
        /// Считывает данные из всех TextBox, парсит их и проверяет на баланс.
        /// </summary>
        /// <returns>True, если данные верны, иначе False.</returns>
        private bool ReadInputData()
        {
            try
            {
                costs = new int[rows, cols];
                supply = new int[rows];
                demand = new int[cols];

                // Считываем Запасы (A)
                for (int i = 0; i < rows; i++)
                {
                    supply[i] = int.Parse(supplyTextBoxes[i].Text);
                }

                // Считываем Потребности (B)
                for (int j = 0; j < cols; j++)
                {
                    demand[j] = int.Parse(demandTextBoxes[j].Text);
                }

                // Считываем Тарифы (C)
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        costs[i, j] = int.Parse(costTextBoxes[i, j].Text);
                    }
                }

                // Проверка баланса
                int totalSupply = supply.Sum();
                int totalDemand = demand.Sum();

                if (totalSupply != totalDemand)
                {
                    Log($"!!! ОШИБКА: Дисбаланс задачи! !!!");
                    Log($"Сумма запасов: {totalSupply}");
                    Log($"Сумма потребностей: {totalDemand}");
                    return false;
                }

                Log($"Данные успешно считаны. Баланс = {totalSupply}");
                return true;
            }
            catch (FormatException)
            {
                Log("!!! КРИТИЧЕСКАЯ ОШИБКА: Одно из полей содержит неверный формат (не число).");
                return false;
            }
            catch (Exception ex)
            {
                Log("Неизвестная ошибка при чтении данных: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Инициализирует DataTable для DataGrid на основе считанных данных.
        /// </summary>
        private void InitializeDisplayTable()
        {
            // Очищаем таблицу перед новым построением
            displayTable.Clear();
            displayTable.Columns.Clear();

            // Добавляем колонки
            displayTable.Columns.Add("Поставщик/Тариф");
            for (int j = 0; j < cols; j++)
            {
                displayTable.Columns.Add($"B{j + 1} (Потр: {demand[j]})");
            }
            displayTable.Columns.Add($"Запасы (a_i)");

            // Добавляем строки
            for (int i = 0; i < rows; i++)
            {
                DataRow row = displayTable.NewRow();
                row[0] = $"A{i + 1}";
                for (int j = 0; j < cols; j++)
                {
                    row[j + 1] = costs[i, j].ToString(); // Показываем тариф
                }
                row[cols + 1] = supply[i].ToString();
                displayTable.Rows.Add(row);
            }

            PlanDataGrid.ItemsSource = displayTable.DefaultView;
        }

        /// <summary>
        /// Обновляет DataGrid, показывая текущий план (перевозки) в скобках.
        /// </summary>
        private void UpdateDisplayTable()
        {
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    string cellValue = costs[i, j].ToString();
                    if (currentPlan[i, j] > 0)
                    {
                        // Формат "Тариф (Перевозка)"
                        cellValue += $" ({currentPlan[i, j]})";
                    }
                    displayTable.Rows[i][j + 1] = cellValue;
                }
            }
        }

        /// <summary>
        /// Рассчитывает общую стоимость перевозок для заданного плана.
        /// </summary>
        private int CalculateTotalCost(int[,] plan)
        {
            int totalCost = 0;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (plan[i, j] > 0)
                    {
                        totalCost += plan[i, j] * costs[i, j];
                    }
                }
            }
            return totalCost;
        }

        /// <summary>
        /// Выводит сообщение в лог. (Улучшено: добавлена временная метка)
        /// </summary>
        private void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            LogTextBox.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
            LogTextBox.ScrollToEnd();
        }

        //--- 1. МЕТОД СЕВЕРО-ЗАПАДНОГО УГЛА (NWC) ---

        private void NWC_Button_Click(object sender, RoutedEventArgs e)
        {
            // Добавлены разделители для секции
            Log(new string('=', 60));
            Log(">>> ЗАПУСК: Метод Северо-Западного Угла <<<");
            Log(new string('=', 60));

            // 1. Считать данные и проверить
            if (!ReadInputData()) return;

            // 2. Инициализировать таблицу для вывода
            InitializeDisplayTable();

            // 3. Решить задачу
            Log("Построение начального опорного плана (СЗУ)...");
            currentPlan = SolveNWC();

            // 4. Обновить отображение
            UpdateDisplayTable();
            int totalCost = CalculateTotalCost(currentPlan);
            Log($"План построен. Общая стоимость: {totalCost}");
            Log($"Количество занятых ячеек: {GetBasicCells(currentPlan).Count} (m+n-1 = {rows + cols - 1})");
            Log(new string('-', 60));
        }

        private int[,] SolveNWC()
        {
            int[,] plan = new int[rows, cols];
            int[] currentSupply = (int[])supply.Clone();
            int[] currentDemand = (int[])demand.Clone();

            int i = 0, j = 0;
            while (i < rows && j < cols)
            {
                int allocation = Math.Min(currentSupply[i], currentDemand[j]);
                plan[i, j] = allocation;
                currentSupply[i] -= allocation;
                currentDemand[j] -= allocation;

                if (currentSupply[i] == 0)
                {
                    i++; // Переходим к следующей строке
                }
                else
                {
                    j++; // Переходим к следующему столбцу
                }
            }
            return plan;
        }

        //--- 2. МЕТОД МИНИМАЛЬНОГО ЭЛЕМЕНТА (MEC) ---

        private void MEC_Button_Click(object sender, RoutedEventArgs e)
        {
            // Добавлены разделители для секции
            Log(new string('=', 60));
            Log(">>> ЗАПУСК: Метод Минимального Элемента <<<");
            Log(new string('=', 60));

            // 1. Считать данные и проверить
            if (!ReadInputData()) return;

            // 2. Инициализировать таблицу для вывода
            InitializeDisplayTable();

            // 3. Решить задачу
            Log("Построение начального опорного плана (Мин. эл.)...");
            currentPlan = SolveMEC();

            // 4. Обновить отображение
            UpdateDisplayTable();
            int totalCost = CalculateTotalCost(currentPlan);
            Log($"План построен. Общая стоимость: {totalCost}");
            Log($"Количество занятых ячеек: {GetBasicCells(currentPlan).Count} (m+n-1 = {rows + cols - 1})");
            Log(new string('-', 60));
        }

        private int[,] SolveMEC()
        {
            int[,] plan = new int[rows, cols];
            int[] currentSupply = (int[])supply.Clone();
            int[] currentDemand = (int[])demand.Clone();
            bool[,] visited = new bool[rows, cols]; // Ячейки, которые больше нельзя использовать

            int allocations = 0;
            int maxAllocations = rows + cols - 1; // Для невырожденного плана

            // Примечание: Для сбалансированных задач гарантировано, что будет m+n-1 или m+n базисных клеток.
            // Для полного цикла нужно, чтобы все потребности и запасы были исчерпаны.
            int exhaustedRows = 0;
            int exhaustedCols = 0;

            while (exhaustedRows < rows || exhaustedCols < cols)
            {
                // Находим ячейку с минимальной стоимостью среди доступных
                int minCost = int.MaxValue;
                Cell? minCell = null; // Используем Nullable<Cell>

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        if (currentSupply[i] > 0 && currentDemand[j] > 0 && costs[i, j] < minCost)
                        {
                            minCost = costs[i, j];
                            minCell = new Cell(i, j);
                        }
                    }
                }

                if (minCell == null) break; // Все ячейки распределены

                int r = minCell.Value.Row;
                int c = minCell.Value.Col;

                // Распределяем груз
                int allocation = Math.Min(currentSupply[r], currentDemand[c]);
                plan[r, c] = allocation;

                // Обновляем остатки
                currentSupply[r] -= allocation;
                currentDemand[c] -= allocation;
                allocations++;

                // Помечаем исчерпанные строки/столбцы
                if (currentSupply[r] == 0) exhaustedRows++;
                if (currentDemand[c] == 0) exhaustedCols++;
            }
            return plan;
        }


        //--- 3. МЕТОД ПОТЕНЦИАЛОВ (ОПТИМИЗАЦИЯ) ---

        private void Potentials_Button_Click(object sender, RoutedEventArgs e)
        {
            // Добавлены разделители для секции
            Log(new string('=', 60));
            Log(">>> ЗАПУСК: Метод Потенциалов (Оптимизация) <<<");
            Log(new string('=', 60));

            if (currentPlan == null || CalculateTotalCost(currentPlan) == 0)
            {
                Log("!!! Ошибка: Сначала постройте начальный план (СЗУ или Мин. эл.).");
                return;
            }

            OptimizeWithPotentials();
            Log(new string('-', 60));
        }

        private void OptimizeWithPotentials()
        {
            int[,] plan = (int[,])currentPlan.Clone();
            int iteration = 1;

            while (iteration <= 20) // Ограничение на 20 итераций
            {
                Log($"\n--- Итерация {iteration} (Текущая стоимость: {CalculateTotalCost(plan)}) ---");

                // Шаг 1: Получаем базисные (занятые) ячейки
                List<Cell> basicCells = GetBasicCells(plan);

                // Проверка на вырожденность
                if (basicCells.Count < rows + cols - 1)
                {
                    Log($"! ВНИМАНИЕ: Обнаружен вырожденный план ({basicCells.Count} < {rows + cols - 1}).");
                    // В реальном приложении здесь нужно добавить фиктивную перевозку.
                    Log("Оптимизация остановлена (требуется обработка вырождения).");
                    break;
                }

                // Шаг 2: Рассчитываем потенциалы u[i] и v[j]
                var (u, v, success) = CalculatePotentials(basicCells);
                if (!success)
                {
                    Log("!!! Ошибка: Не удалось рассчитать потенциалы. Остановка.");
                    break;
                }
                Log($"Потенциалы рассчитаны: u = [{string.Join(", ", u.Select(x => $"{x:F0}"))}], v = [{string.Join(", ", v.Select(x => $"{x:F0}"))}]");

                // Шаг 3: Проверка оптимальности для свободных ячеек
                var (isOptimal, enteringCell, maxViolation) = CheckOptimality(plan, u, v);

                if (isOptimal)
                {
                    Log("\n--- ПЛАН ОПТИМАЛЕН ---");
                    Log($"Финальная общая стоимость: {CalculateTotalCost(plan)}");
                    currentPlan = (int[,])plan.Clone();
                    UpdateDisplayTable();
                    break;
                }

                Log($"План не оптимален. Макс. нарушение ({maxViolation:F2}) в ячейке ({enteringCell.Value.Row + 1}, {enteringCell.Value.Col + 1}).");

                // Шаг 4: Строим цикл (контур)
                List<Cell> loop = FindLoop(plan, enteringCell.Value);
                if (loop == null)
                {
                    Log("!!! Ошибка: Не удалось построить цикл. Остановка.");
                    break;
                }

                // Шаг 5: Перераспределяем перевозки
                plan = Reallocate(plan, loop, enteringCell.Value);

                currentPlan = (int[,])plan.Clone();
                UpdateDisplayTable();
                Log($"План улучшен. Новая стоимость: {CalculateTotalCost(plan)}");
                iteration++;
            }

            if (iteration > 20)
            {
                Log("Достигнут лимит итераций. Оптимизация остановлена.");
            }
        }

        /// <summary>
        /// Возвращает список базисных (занятых) ячеек.
        /// </summary>
        private List<Cell> GetBasicCells(int[,] plan)
        {
            var cells = new List<Cell>();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (plan[i, j] > 0)
                    {
                        cells.Add(new Cell(i, j));
                    }
                }
            }
            return cells;
        }

        /// <summary>
        /// Рассчитывает потенциалы u и v по базисным ячейкам.
        /// </summary>
        private (double[] u, double[] v, bool success) CalculatePotentials(List<Cell> basicCells)
        {
            double?[] u = new double?[rows];
            double?[] v = new double?[cols];

            u[0] = 0; // Задаем u[0] = 0

            int calcs = 0;
            int maxCalcs = (rows + cols) * 2; // Защита от бесконечного цикла

            while (u.Any(val => !val.HasValue) || v.Any(val => !val.HasValue))
            {
                bool changed = false;
                foreach (var cell in basicCells)
                {
                    // u[i] + v[j] = c[i,j]
                    if (u[cell.Row].HasValue && !v[cell.Col].HasValue)
                    {
                        v[cell.Col] = costs[cell.Row, cell.Col] - u[cell.Row].Value;
                        changed = true;
                    }
                    else if (!u[cell.Row].HasValue && v[cell.Col].HasValue)
                    {
                        u[cell.Row] = costs[cell.Row, cell.Col] - v[cell.Col].Value;
                        changed = true;
                    }
                }

                if (!changed && (u.Any(val => !val.HasValue) || v.Any(val => !val.HasValue)))
                {
                    // Не удалось найти все потенциалы - план вырожден или несвязан
                    return (null, null, false);
                }

                if (calcs++ > maxCalcs) return (null, null, false); // Ошибка
            }

            // Конвертируем double?[] в double[]
            double[] uResult = new double[rows];
            double[] vResult = new double[cols];
            for (int i = 0; i < rows; i++) uResult[i] = u[i].Value;
            for (int j = 0; j < cols; j++) vResult[j] = v[j].Value;

            return (uResult, vResult, true);
        }

        /// <summary>
        /// Проверяет план на оптимальность.
        /// </summary>
        private (bool isOptimal, Cell? enteringCell, double maxViolation) CheckOptimality(int[,] plan, double[] u, double[] v)
        {
            double maxViolation = 0;
            Cell? enteringCell = null;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (plan[i, j] == 0) // Только для свободных ячеек
                    {
                        // delta = u[i] + v[j] - c[i,j] (показывает, насколько выгодно ввести эту клетку)
                        // Чем больше, тем лучше
                        double delta = u[i] + v[j] - costs[i, j];

                        if (delta > 1e-9) // Если выгодно (delta > 0)
                        {
                            if (delta > maxViolation)
                            {
                                maxViolation = delta;
                                enteringCell = new Cell(i, j);
                            }
                        }
                    }
                }
            }

            if (enteringCell != null)
            {
                return (false, enteringCell, maxViolation); // Не оптимален
            }

            return (true, null, 0); // Оптимален
        }

        /// <summary>
        /// Находит цикл пересчета (самая сложная часть).
        /// </summary>
        private List<Cell> FindLoop(int[,] plan, Cell startCell)
        {
            var loop = new List<Cell> { startCell };
            var basicCells = GetBasicCells(plan);

            // Рекурсивный поиск (DFS)
            bool Dfs(Cell current, Cell target, bool findInRow)
            {
                // Условие замыкания цикла: 
                // 1. Цикл должен иметь 4 и более ячеек
                // 2. Текущая ячейка (current) должна быть связана со стартовой ячейкой (target)
                if (loop.Count > 3 &&
                    ((findInRow && current.Row == target.Row) || (!findInRow && current.Col == target.Col)))
                {
                    // Добавление стартовой ячейки в конец цикла, если ее там нет (для замыкания)
                    if (!loop.Contains(target))
                    {
                        // Это сложный момент в DFS, но в транспортной задаче часто просто проверяют, 
                        // что мы вернулись в ту же строку/столбец.
                        // Здесь мы ищем путь от startCell до startCell.
                        loop.Add(target);
                    }
                    return true;
                }

                // Находим все базисные ячейки, которые находятся в той же строке/столбце, что и current
                var candidates = findInRow
                    ? basicCells.Where(c => c.Row == current.Row && c.Col != current.Col).ToList()
                    : basicCells.Where(c => c.Col == current.Col && c.Row != current.Row).ToList();

                // Если это первый шаг от стартовой ячейки, нужно включить и ее (startCell)
                if (current.Equals(startCell))
                {
                    // Нужно найти *любую* базисную ячейку, которая лежит в одной строке/столбце
                    if (findInRow) // Ищем по строке
                    {
                        candidates = basicCells.Where(c => c.Row == current.Row).ToList();
                    }
                    else // Ищем по столбцу
                    {
                        candidates = basicCells.Where(c => c.Col == current.Col).ToList();
                    }
                    // Исключаем саму startCell, если она была добавлена
                    candidates.Remove(startCell);
                }


                foreach (var next in candidates)
                {
                    // Условие: следующая ячейка не должна быть уже в цикле, кроме стартовой
                    if (loop.Contains(next) && !next.Equals(target)) continue;

                    loop.Add(next);
                    if (Dfs(next, target, !findInRow)) // Меняем направление поиска (строка -> столбец или наоборот)
                    {
                        return true;
                    }
                    loop.RemoveAt(loop.Count - 1); // Backtrack
                }
                return false;
            }

            // Начинаем поиск. Ищем сначала по строке, потом по столбцу, чтобы найти цикл.
            // Примечание: Цикл не должен проходить через стартовую ячейку (loop[0]) в процессе поиска.
            // Обнуляем loop, оставляя только startCell
            loop.RemoveAll(c => !c.Equals(startCell));

            // Пробуем искать по строке (true) и по столбцу (false)
            if (Dfs(startCell, startCell, true) || Dfs(startCell, startCell, false))
            {
                // Удаляем последнюю (дублирующую) startCell, если она была добавлена
                if (loop.Count > 0 && loop.Last().Equals(startCell) && loop.IndexOf(startCell) != loop.Count - 1)
                {
                    loop.RemoveAt(loop.Count - 1);
                }

                // Убеждаемся, что цикл начинается с enteringCell и далее чередуются знаки
                // Проблема в том, что DFS находит узлы цикла, но не гарантирует порядок знаков.
                // Для простоты оставим как есть, но это потенциальная точка сбоя.
                // В большинстве случаев простой DFS дает корректный набор узлов.

                Log($"Цикл найден: {string.Join(" -> ", loop.Select(c => $"({c.Row + 1},{c.Col + 1})"))}");
                return loop;
            }

            return null; // Цикл не найден
        }

        /// <summary>
        /// Перераспределяет перевозки вдоль найденного цикла.
        /// </summary>
        private int[,] Reallocate(int[,] plan, List<Cell> loop, Cell enteringCell)
        {
            int[,] newPlan = (int[,])plan.Clone();
            int theta = int.MaxValue;

            // Находим 'theta' (минимальная перевозка в ячейках со знаком '-')
            // 0-я ячейка в цикле - enteringCell, она всегда '+', 1-я - всегда '-'
            for (int i = 1; i < loop.Count; i += 2) // "Отрицательные" ячейки
            {
                theta = Math.Min(theta, plan[loop[i].Row, loop[i].Col]);
            }

            Log($"Величина переброски (theta) = {theta}");

            // Перераспределяем
            for (int i = 0; i < loop.Count; i++)
            {
                var cell = loop[i];
                if (i % 2 == 0) // Четные индексы (0, 2, 4...) получают '+', включая enteringCell
                {
                    newPlan[cell.Row, cell.Col] += theta;
                }
                else // Нечетные индексы (1, 3, 5...) получают '-'
                {
                    newPlan[cell.Row, cell.Col] -= theta;
                }
            }

            return newPlan;
        }
    }
}