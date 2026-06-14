using System.IO;
using System.Text.Json;
using GsmCalculator.Models;

namespace GsmCalculator.Services;

/// <inheritdoc/>
public class WidgetService : IWidgetService
{
    private readonly string _filePath;
    private readonly List<Widget> _widgets;

    public WidgetService(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _widgets = Load();
    }

    public IReadOnlyList<Widget> GetAll() => _widgets;

    public Widget? Find(Guid id) => _widgets.FirstOrDefault(w => w.Id == id);

    public void Add(Widget widget)
    {
        if (widget is null) throw new ArgumentNullException(nameof(widget));
        if (string.IsNullOrWhiteSpace(widget.Name))
            throw new ArgumentException("Имя виджета не может быть пустым.", nameof(widget));

        _widgets.Add(widget);
        SaveAll();
    }

    public void Update(Widget widget)
    {
        if (widget is null) throw new ArgumentNullException(nameof(widget));
        if (string.IsNullOrWhiteSpace(widget.Name))
            throw new ArgumentException("Имя виджета не может быть пустым.", nameof(widget));

        var existing = Find(widget.Id);
        if (existing is null) return;

        // Обновляем поля, но IsBuiltIn НЕ трогаем — инвариант сервиса:
        // встроенный виджет остаётся встроенным даже после редактирования,
        // т.е. его всё равно нельзя удалить. ViewModel не сможет «случайно
        // обнулить» этот флаг через подсунутый widget.IsBuiltIn=false.
        existing.Name = widget.Name;
        existing.DensityMode = widget.DensityMode;
        existing.DefaultDensity = widget.DefaultDensity;
        existing.DefaultDecimalPlaces = widget.DefaultDecimalPlaces;

        SaveAll();
    }

    public void Remove(Guid widgetId)
    {
        var w = Find(widgetId);
        if (w is null) return;

        if (w.IsBuiltIn)
            throw new InvalidOperationException("Встроенный виджет нельзя удалить.");

        _widgets.Remove(w);
        SaveAll();
    }

    public void SaveAll()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(_widgets, JsonOptions.Default);
        File.WriteAllText(_filePath, json);
    }

    /// <summary>
    /// Загружает список из файла. Если файла нет — сеет встроенные виджеты
    /// и сразу пишет файл (по решению пользователя: «и в коде, и в widgets.json»).
    /// </summary>
    private List<Widget> Load()
    {
        if (File.Exists(_filePath))
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                var loaded = JsonSerializer.Deserialize<List<Widget>>(json, JsonOptions.Default);
                if (loaded is { Count: > 0 })
                    return loaded;
            }
            catch
            {
                // Файл повреждён — пересеем дефолтами ниже.
            }
        }

        var defaults = CreateBuiltInWidgets();
        // Сохранить сразу же, чтобы файл существовал.
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(defaults, JsonOptions.Default));

        return defaults;
    }

    /// <summary>
    /// Заводская конфигурация встроенных виджетов по ТЗ.
    /// </summary>
    private static List<Widget> CreateBuiltInWidgets() => new()
    {
        // Плотности для АИ/ДТ/ТС — расчётные величины, используемые
        // когда фактическая плотность топлива неизвестна.
        new Widget { Name = "АИ-92",  DensityMode = DensityMode.Variable, DefaultDensity = 0.75, DefaultDecimalPlaces = 2, IsBuiltIn = true },
        new Widget { Name = "ДТ-Л",   DensityMode = DensityMode.Variable, DefaultDensity = 0.85, DefaultDecimalPlaces = 2, IsBuiltIn = true },
        new Widget { Name = "ДТ-З",   DensityMode = DensityMode.Variable, DefaultDensity = 0.85, DefaultDecimalPlaces = 2, IsBuiltIn = true },
        new Widget { Name = "ТС-1",   DensityMode = DensityMode.Variable, DefaultDensity = 0.81, DefaultDecimalPlaces = 2, IsBuiltIn = true },
        new Widget { Name = "Масла",  DensityMode = DensityMode.Fixed,    DefaultDensity = 0.9,  DefaultDecimalPlaces = 2, IsBuiltIn = true },
        new Widget { Name = "ТЖ",     DensityMode = DensityMode.Fixed,    DefaultDensity = 1.1,  DefaultDecimalPlaces = 2, IsBuiltIn = true },
        new Widget { Name = "ОЖ",     DensityMode = DensityMode.Fixed,    DefaultDensity = 1.1,  DefaultDecimalPlaces = 2, IsBuiltIn = true },
    };
}
