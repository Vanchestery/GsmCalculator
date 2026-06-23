using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Input;
using GsmCalculator.Helpers;
using GsmCalculator.Models;
using GsmCalculator.Services;

namespace GsmCalculator.ViewModels;

/// <summary>
/// ViewModel главного окна. Поддерживает два режима калькулятора:
/// Classic (как Windows Calc Standard — слева направо) и
/// Engineering (с приоритетом × и ÷ над + и −).
///
/// Также показывает «превью выражения» сверху дисплея, чтобы пользователь
/// видел собираемую цепочку (особенно полезно в Engineering-режиме).
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly ICalculatorService _calc;
    private readonly ISettingsService _settings;
    private readonly IAddWidgetWindowService _addWidgetWindow;
    private readonly ISettingsWindowService _settingsWindow;
    private readonly ILocalizationService _loc;

    private CalculatorMode _mode;
    private RoundingMode _roundingMode;

    // --- Classic mode state ---
    private double _leftOperand;
    private char? _pendingOp;

    // --- Engineering mode state ---
    private readonly List<double> _engOperands = new();
    private readonly List<char> _engOps = new();

    // --- Общее состояние ---
    private bool _isNewNumber = true;
    private bool _justEvaluated;   // true сразу после = — чтобы следующая цифра очистила preview
    private bool _isError;         // true когда на дисплее сообщение об ошибке.
                                   // Флаг вместо сравнения строки — текст «Ошибка» локализован.

    private string _display = "0";
    public string Display
    {
        get => _display;
        set => SetProperty(ref _display, value);
    }

    private string _expressionPreview = "";
    /// <summary>Превью выражения над дисплеем («5 − 1 × 3 =» и т.п.).</summary>
    public string ExpressionPreview
    {
        get => _expressionPreview;
        set => SetProperty(ref _expressionPreview, value);
    }

    public ObservableCollection<HistoryEntry> History { get; } = new();
    public int HistorySize { get; private set; }
    public CalculatorMode Mode => _mode;

    /// <summary>
    /// Текущий режим округления (None / Integer / OneTenth). Управляется
    /// циклической кнопкой в топ-баре. При смене — сохраняется в settings.json.
    /// </summary>
    public RoundingMode RoundingMode
    {
        get => _roundingMode;
        private set
        {
            if (SetProperty(ref _roundingMode, value))
            {
                OnPropertyChanged(nameof(RoundingIndicator));
                OnPropertyChanged(nameof(RoundingTooltip));
            }
        }
    }

    /// <summary>Короткая подпись на кнопке («∞», «1», «0.1»).</summary>
    public string RoundingIndicator => RoundingFormatter.Indicator(_roundingMode);

    /// <summary>Локализованный тултип («Округление: до целых» и т.д.).</summary>
    public string RoundingTooltip => _loc.Get(_roundingMode switch
    {
        Models.RoundingMode.None     => "Main_Rounding_None",
        Models.RoundingMode.Integer  => "Main_Rounding_Integer",
        Models.RoundingMode.OneTenth => "Main_Rounding_OneTenth",
        _                            => "Main_Rounding_None"
    });

    private bool _isHistoryVisible = true;
    /// <summary>Видна ли панель истории. Состояние сохраняется в window-state.json.</summary>
    public bool IsHistoryVisible
    {
        get => _isHistoryVisible;
        set
        {
            if (SetProperty(ref _isHistoryVisible, value))
                OnPropertyChanged(nameof(HistoryToggleLabel));
        }
    }

    /// <summary>
    /// Локализованная подпись кнопки переключения истории.
    /// Реактивно меняется при смене IsHistoryVisible и при смене языка
    /// (см. подписку на LanguageChanged в конструкторе).
    /// </summary>
    public string HistoryToggleLabel
        => _loc.Get(_isHistoryVisible ? "Main_HideHistory" : "Main_ShowHistory");

    // --- Команды ---
    public ICommand DigitCommand { get; }
    public ICommand DecimalCommand { get; }
    public ICommand OperationCommand { get; }
    public ICommand EqualsCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand ClearEntryCommand { get; }
    public ICommand BackspaceCommand { get; }
    public ICommand NegateCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenAddWidgetCommand { get; }
    public ICommand ToggleHistoryCommand { get; }
    public ICommand CycleRoundingModeCommand { get; }

    public MainViewModel(
        ICalculatorService calc,
        ISettingsService settings,
        IAddWidgetWindowService addWidgetWindow,
        ISettingsWindowService settingsWindow,
        ILocalizationService localization)
    {
        _calc = calc;
        _settings = settings;
        _addWidgetWindow = addWidgetWindow;
        _settingsWindow = settingsWindow;
        _loc = localization;

        var loaded = settings.Load();
        HistorySize = loaded.HistorySize;
        _mode = loaded.CalculatorMode;
        _roundingMode = loaded.RoundingMode;

        DigitCommand = new RelayCommand<string>(AppendDigit);
        DecimalCommand = new RelayCommand(_ => AppendDecimal());
        OperationCommand = new RelayCommand<string>(HandleOperation);
        EqualsCommand = new RelayCommand(_ => HandleEquals());
        ClearCommand = new RelayCommand(_ => Clear());
        ClearEntryCommand = new RelayCommand(_ => ClearEntry());
        BackspaceCommand = new RelayCommand(_ => Backspace());
        NegateCommand = new RelayCommand(_ => Negate());

        OpenSettingsCommand = new RelayCommand(_ => _settingsWindow.OpenDialog());
        OpenAddWidgetCommand = new RelayCommand(_ => _addWidgetWindow.OpenDialog());
        ToggleHistoryCommand = new RelayCommand(_ => IsHistoryVisible = !IsHistoryVisible);
        CycleRoundingModeCommand = new RelayCommand(_ => CycleRoundingMode());

        // MainViewModel — singleton, поэтому отписка не нужна (живёт до конца процесса).
        // При смене языка перерисовываем подписи привязанных строк.
        _loc.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HistoryToggleLabel));
            OnPropertyChanged(nameof(RoundingTooltip));
        };
    }

    // =================================================================
    // Публичные API для других ViewModels
    // =================================================================

    public double GetDisplayValue()
        => double.TryParse(_display, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

    /// <summary>
    /// Помечает что текущее значение дисплея было «использовано» внешним
    /// потребителем (например, виджет прочитал его через GetDisplayValue
    /// для конвертации). Следующий ввод цифры будет начинать новое число,
    /// а не дописывать к текущему — как после нажатия =.
    ///
    /// _pendingOp намеренно НЕ очищается: если пользователь был в середине
    /// выражения «5 + 100», взял 100 в виджет и потом ввёл 200, ожидается
    /// что = даст 5 + 200, а не сброс операции.
    /// </summary>
    public void NotifyDisplayConsumed()
    {
        _isNewNumber = true;
    }

    public void SetDisplayValue(double value)
    {
        // Применяем округление по текущему режиму — виджет «Вставить в калькулятор»
        // даёт точное число конвертации, но юзер хочет увидеть его уже округлённым
        // (по K.4 — в учёте ГСМ работают именно с округлённой величиной).
        var rounded = RoundingFormatter.Apply(value, _roundingMode);
        Display = _calc.FormatNumber(rounded);
        ResetCalculationState();
        ExpressionPreview = "";
        _justEvaluated = false;
        _isNewNumber = true;
        _isError = false;
    }

    public void ApplyHistorySize(int newSize)
    {
        HistorySize = Math.Max(1, newSize);
        TrimHistory();
    }

    public void ApplyCalculatorMode(CalculatorMode mode)
    {
        if (_mode == mode) return;
        _mode = mode;
        Clear();
    }

    /// <summary>
    /// Циклически переключает режим округления (None → Integer → OneTenth → None).
    /// Сохраняет новый режим в settings.json через ISettingsService.
    ///
    /// Не пересчитывает то что уже на дисплее — округление применяется только
    /// к НОВЫМ результатам (по K.5 = Y «округление в момент =»). История тоже
    /// не пересчитывается (по K.6 — округление в момент записи).
    /// </summary>
    public void CycleRoundingMode()
    {
        RoundingMode = RoundingFormatter.Next(_roundingMode);

        // Сохраняем. Load+Save паттерн используется чтобы не потерять остальные
        // поля настроек (тема, язык и т.д., которые меняются в SettingsViewModel).
        var s = _settings.Load();
        s.RoundingMode = _roundingMode;
        _settings.Save(s);
    }

    /// <summary>Значение дисплея для сохранения в сессию. В состоянии ошибки — «0».</summary>
    public string GetSessionDisplay() => _isError ? "0" : _display;

    /// <summary>Снимок истории для сохранения в сессию.</summary>
    public IReadOnlyList<HistoryEntry> GetHistorySnapshot() => History.ToList();

    /// <summary>Восстанавливает дисплей и историю из сохранённой сессии.</summary>
    public void RestoreSession(string display, IEnumerable<HistoryEntry> history)
    {
        // Восстанавливаем дисплей только если это валидное число.
        Display = double.TryParse(display, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
            ? display
            : "0";

        History.Clear();
        foreach (var entry in history)
            History.Add(entry);
        TrimHistory();

        // Сбрасываем состояние вычисления — восстановленное число
        // ведёт себя как готовый результат (следующая цифра начинает новое).
        ResetCalculationState();
        ExpressionPreview = "";
        _isNewNumber = true;
        _justEvaluated = false;
        _isError = false;
    }

    // =================================================================
    // Ввод цифр / точки
    // =================================================================

    private void AppendDigit(string? digit)
    {
        if (string.IsNullOrEmpty(digit)) return;

        // После = — следующая цифра начинает новое вычисление, чистим превью.
        if (_justEvaluated)
        {
            ExpressionPreview = "";
            _justEvaluated = false;
        }

        if (_isNewNumber || _display == "0" || _isError)
        {
            Display = digit;
            _isNewNumber = false;
            _isError = false;
            return;
        }

        if (_display.Length >= 16) return;
        Display = _display + digit;
    }

    private void AppendDecimal()
    {
        if (_justEvaluated)
        {
            ExpressionPreview = "";
            _justEvaluated = false;
        }

        if (_isNewNumber || _isError)
        {
            Display = "0.";
            _isNewNumber = false;
            _isError = false;
            return;
        }

        if (!_display.Contains('.'))
            Display = _display + ".";
    }

    // =================================================================
    // Диспетчер
    // =================================================================

    private void HandleOperation(string? op)
    {
        if (string.IsNullOrEmpty(op) || op.Length != 1) return;
        var c = op[0];

        if (_mode == CalculatorMode.Classic)
            HandleOperationClassic(c);
        else
            HandleOperationEngineering(c);
    }

    private void HandleEquals()
    {
        if (_mode == CalculatorMode.Classic)
            HandleEqualsClassic();
        else
            HandleEqualsEngineering();
    }

    // =================================================================
    // CLASSIC MODE
    // =================================================================

    private void HandleOperationClassic(char op)
    {
        if (_isError) { ClearClassicState(); _isNewNumber = true; return; }
        _justEvaluated = false; // оператор после = — начало новой цепочки

        if (_pendingOp.HasValue && !_isNewNumber)
        {
            if (!TryEvaluatePendingClassic()) return;
        }
        else
        {
            _leftOperand = GetDisplayValue();
        }

        _pendingOp = op;
        _isNewNumber = true;
        ExpressionPreview = $"{_calc.FormatNumber(_leftOperand)} {op} ";
    }

    private void HandleEqualsClassic()
    {
        if (!_pendingOp.HasValue) return;

        // Сохраняем «было» для превью, потому что TryEvaluate меняет _leftOperand.
        var leftBefore = _leftOperand;
        var rightBefore = GetDisplayValue();
        var opBefore = _pendingOp.Value;

        if (!TryEvaluatePendingClassic()) return;

        ExpressionPreview = $"{_calc.FormatExpression(leftBefore, opBefore, rightBefore)} =";
        _justEvaluated = true;
        _pendingOp = null;
        _isNewNumber = true;
    }

    private bool TryEvaluatePendingClassic()
    {
        if (!_pendingOp.HasValue) return true;

        var right = GetDisplayValue();
        var op = _pendingOp.Value;

        try
        {
            var result = _calc.Apply(_leftOperand, op, right);
            // K.5=Y: округляем «на фиксировании» — это и нажатие =,
            // и нажатие следующего оператора (он закрывает предыдущий операнд).
            // Аккумулятор уносит уже округлённое — дальнейшая цепочка считает с него.
            result = RoundingFormatter.Apply(result, _roundingMode);
            AddToHistory(_calc.FormatExpression(_leftOperand, op, right), _calc.FormatNumber(result));
            Display = _calc.FormatNumber(result);
            _leftOperand = result;
            return true;
        }
        catch (DivideByZeroException)
        {
            ShowError();
            return false;
        }
    }

    // =================================================================
    // ENGINEERING MODE
    // =================================================================

    private void HandleOperationEngineering(char op)
    {
        if (_isError) { ClearEngineeringState(); _isNewNumber = true; return; }
        _justEvaluated = false;

        if (_isNewNumber)
        {
            if (_engOps.Count > 0)
            {
                // Оператор сразу после оператора — заменяем последний.
                _engOps[^1] = op;
            }
            else
            {
                // Самый первый ввод — текущий display становится первым операндом.
                _engOperands.Add(GetDisplayValue());
                _engOps.Add(op);
            }
        }
        else
        {
            // Только что вводили цифры — фиксируем операнд + оператор.
            _engOperands.Add(GetDisplayValue());
            _engOps.Add(op);
            _isNewNumber = true;
        }

        UpdateEngineeringPreview();
    }

    private void HandleEqualsEngineering()
    {
        if (_engOps.Count == 0) return;
        if (_isError) return;

        var finalOperand = GetDisplayValue();
        var allOperands = new List<double>(_engOperands) { finalOperand };
        var allOps = new List<char>(_engOps);

        var expression = BuildEngineeringExpression(allOperands, allOps);

        try
        {
            var result = EvaluateWithPrecedence(allOperands, allOps);
            // K.5=Y: округление на =. В Engineering выражение считается «одним хапом»,
            // поэтому промежуточные операнды округлению не подвергаются — только итог.
            result = RoundingFormatter.Apply(result, _roundingMode);
            var resultStr = _calc.FormatNumber(result);

            AddToHistory(expression, resultStr);
            Display = resultStr;
            ExpressionPreview = expression + " =";
            _justEvaluated = true;

            ClearEngineeringState();
            _isNewNumber = true;
        }
        catch (DivideByZeroException)
        {
            ShowError();
        }
    }

    /// <summary>
    /// Превью для Engineering-режима: «5 − 1 × 3 » (с висящей операцией если есть).
    /// Вызывается после каждого изменения списков операндов/операторов.
    /// </summary>
    private void UpdateEngineeringPreview()
    {
        if (_engOperands.Count == 0)
        {
            ExpressionPreview = "";
            return;
        }

        var sb = new StringBuilder();
        sb.Append(_calc.FormatNumber(_engOperands[0]));

        for (int i = 0; i < _engOps.Count; i++)
        {
            sb.Append(' ').Append(_engOps[i]);

            // Если есть операнд после этого оператора — печатаем.
            // Если оператор последний и операнд ещё не введён — оставляем висящий «5 −».
            if (i + 1 < _engOperands.Count)
                sb.Append(' ').Append(_calc.FormatNumber(_engOperands[i + 1]));
        }

        sb.Append(' '); // визуальный «зазор» что ждём следующий ввод
        ExpressionPreview = sb.ToString();
    }

    private double EvaluateWithPrecedence(List<double> operands, List<char> ops)
    {
        for (int i = 0; i < ops.Count; )
        {
            var c = ops[i];
            if (c is '×' or '*' or '÷' or '/')
            {
                var r = _calc.Apply(operands[i], c, operands[i + 1]);
                operands[i] = r;
                operands.RemoveAt(i + 1);
                ops.RemoveAt(i);
            }
            else i++;
        }

        var acc = operands[0];
        for (int i = 0; i < ops.Count; i++)
            acc = _calc.Apply(acc, ops[i], operands[i + 1]);
        return acc;
    }

    private string BuildEngineeringExpression(List<double> operands, List<char> ops)
    {
        if (operands.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.Append(_calc.FormatNumber(operands[0]));
        for (int i = 0; i < ops.Count; i++)
        {
            sb.Append(' ').Append(ops[i]).Append(' ');
            if (i + 1 < operands.Count)
                sb.Append(_calc.FormatNumber(operands[i + 1]));
        }
        return sb.ToString();
    }

    // =================================================================
    // Общие операции
    // =================================================================

    private void Clear()
    {
        Display = "0";
        ResetCalculationState();
        ExpressionPreview = "";
        _justEvaluated = false;
        _isNewNumber = true;
        _isError = false;
    }

    private void ClearEntry()
    {
        Display = "0";
        _isNewNumber = true;
        _isError = false;
        // Превью оставляем — pendingOp/токены не сбрасываем.
    }

    private void Backspace()
    {
        if (_isNewNumber || _display == "0" || _isError) return;

        var isNegativeSingleDigit = _display.Length == 2 && _display[0] == '-';
        if (_display.Length == 1 || isNegativeSingleDigit)
        {
            Display = "0";
            _isNewNumber = true;
            return;
        }

        Display = _display[..^1];
    }

    private void Negate()
    {
        if (_display == "0" || _isError) return;
        Display = _display.StartsWith('-') ? _display[1..] : "-" + _display;
    }

    // =================================================================
    // Хелперы
    // =================================================================

    private void ResetCalculationState()
    {
        ClearClassicState();
        ClearEngineeringState();
    }

    private void ClearClassicState()
    {
        _leftOperand = 0;
        _pendingOp = null;
    }

    private void ClearEngineeringState()
    {
        _engOperands.Clear();
        _engOps.Clear();
    }

    private void ShowError()
    {
        Display = _loc.Get("Common_Error");
        _isError = true;
        ResetCalculationState();
        ExpressionPreview = "";
        _justEvaluated = false;
        _isNewNumber = true;
    }

    private void AddToHistory(string expression, string result)
    {
        History.Add(HistoryEntry.Create(expression, result));
        TrimHistory();
    }

    private void TrimHistory()
    {
        while (History.Count > HistorySize)
            History.RemoveAt(0);
    }
}
