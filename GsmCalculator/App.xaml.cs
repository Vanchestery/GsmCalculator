using System.ComponentModel;
using System.IO;
using System.Windows;
using GsmCalculator.Helpers;
using GsmCalculator.Models;
using GsmCalculator.Services;
using GsmCalculator.ViewModels;
using GsmCalculator.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GsmCalculator;

/// <summary>
/// Точка композиции приложения.
/// Здесь строится DI-контейнер, применяются настройки, решается вопрос
/// восстановления сессии и создаётся главное окно.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Глобальный сервис-провайдер. Доступен из любого места через App.Services
    /// (используется в окнах-виджетах, которые открываются по событию из ViewModel).
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Services = ConfigureServices();

        // 1. Загружаем настройки. Если settings.json не существует — Load() вернёт
        //    дефолты; сохраняем их, чтобы файл появился в %AppData%\GsmCalculator.
        var settingsSvc = Services.GetRequiredService<ISettingsService>();
        var settings = settingsSvc.Load();
        if (!File.Exists(AppPaths.SettingsFile))
            settingsSvc.Save(settings);

        // 2. Применяем тему и язык до показа любых окон.
        Services.GetRequiredService<IThemeService>().ApplyTheme(settings.Theme);
        Services.GetRequiredService<ILocalizationService>().SetLanguage(settings.Language);

        // 3. Создаём главное окно и СРАЗУ назначаем его Application.MainWindow.
        //    Это важно: при ShutdownMode=OnMainWindowClose диалог запуска,
        //    показанный раньше главного окна, иначе мог бы стать MainWindow
        //    и завершить приложение при своём закрытии.
        var mainWindow = new MainWindow
        {
            DataContext = Services.GetRequiredService<MainViewModel>()
        };
        MainWindow = mainWindow;
        mainWindow.Closing += OnMainWindowClosing;

        // 4. Восстанавливаем позицию и размер окна (или центрируем при первом запуске).
        //    Делаем ДО Show — чтобы окно сразу появилось на нужном месте, без «прыжка».
        ApplyWindowState(mainWindow, Services.GetRequiredService<IWindowStateService>().Load());

        // 5. Решаем, восстанавливать ли прошлую сессию (может показать диалог).
        var session = DecideSessionRestore(settings);

        // 6. Показываем главное окно.
        mainWindow.Show();

        // 7. Восстанавливаем сессию ПОСЛЕ показа окна — чтобы виджеты
        //    корректно позиционировались, а биндинги истории обновились.
        if (session != null)
            RestoreSession(session);
    }

    /// <summary>
    /// Применяет сохранённое состояние главного окна: позицию, размер,
    /// maximized-флаг, видимость истории. Если состояние отсутствует или
    /// вне экранов — центрирует окно и сжимает по содержимому (Auto-size).
    /// </summary>
    private static void ApplyWindowState(Window window, MainWindowState? state)
    {
        // IsHistoryVisible применяется к VM в любом случае — она сама
        // через PropertyChanged триггерит physical layout в code-behind.
        if (window.DataContext is MainViewModel vm)
            vm.IsHistoryVisible = state?.IsHistoryVisible ?? true;

        if (state != null && ScreenHelper.IsOnScreen(state.Left, state.Top))
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = state.Left;
            window.Top = state.Top;
            window.Width = state.Width;
            window.Height = state.Height;
            if (state.IsMaximized)
                window.WindowState = System.Windows.WindowState.Maximized;
        }
        else
        {
            // Первый запуск или невалидная позиция:
            // - SizeToContent даёт «Auto»-размер по содержимому (компактнее чем XAML-дефолт)
            // - CenterScreen центрирует с учётом вычисленного размера
            // MainWindow.OnLoaded возвращает SizeToContent в Manual, чтобы дальше
            // пользователь мог ресайзить мышкой.
            window.SizeToContent = SizeToContent.WidthAndHeight;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    /// <summary>
    /// Определяет по StartupBehavior, нужно ли восстанавливать сессию.
    /// Возвращает SessionState для восстановления или null.
    /// </summary>
    private SessionState? DecideSessionRestore(AppSettings settings)
    {
        var sessionService = Services.GetRequiredService<ISessionService>();
        if (!sessionService.HasSavedSession)
            return null;

        var session = sessionService.Load();
        if (session is null)
            return null;

        switch (settings.StartupBehavior)
        {
            case StartupBehavior.AlwaysContinue:
                return session;

            case StartupBehavior.AlwaysFresh:
                sessionService.Clear();
                return null;

            case StartupBehavior.AlwaysAsk:
            default:
                var loc = Services.GetRequiredService<ILocalizationService>();
                var savedAtText = loc.GetFormat("Session_SavedAt", session.SavedAt.ToString("g"));

                var dialog = new ContinueSessionDialog(savedAtText);
                var continueIt = dialog.ShowDialog() == true;

                if (!continueIt)
                {
                    sessionService.Clear();
                    return null;
                }
                return session;
        }
    }

    /// <summary>Восстанавливает дисплей, историю и окна-виджеты из сессии.</summary>
    private void RestoreSession(SessionState session)
    {
        var mainVm = Services.GetRequiredService<MainViewModel>();
        mainVm.RestoreSession(session.CurrentDisplay, session.History);

        var widgetService = Services.GetRequiredService<IWidgetService>();
        var widgetWindows = Services.GetRequiredService<IWidgetWindowService>();

        foreach (var ws in session.OpenWidgets)
        {
            // Виджет мог быть удалён пользователем — тогда просто пропускаем.
            var widget = widgetService.Find(ws.WidgetId);
            if (widget != null)
                widgetWindows.RestoreWidget(widget, ws);
        }
    }

    /// <summary>
    /// При закрытии главного окна сохраняем сессию И состояние окна.
    /// В этот момент виджеты ещё открыты — можно снять их позиции.
    /// Сессия и состояние окна — два независимых файла (см. ТЗ к C),
    /// поэтому сохраняем в двух try-блоках, чтобы сбой одного не отменял другой.
    /// </summary>
    private void OnMainWindowClosing(object? sender, CancelEventArgs e)
    {
        // Сессия (дисплей, история, открытые виджеты)
        try
        {
            var mainVm = Services.GetRequiredService<MainViewModel>();
            var widgetWindows = Services.GetRequiredService<IWidgetWindowService>();
            var sessionService = Services.GetRequiredService<ISessionService>();

            var state = new SessionState
            {
                CurrentDisplay = mainVm.GetSessionDisplay(),
                History = mainVm.GetHistorySnapshot().ToList(),
                OpenWidgets = widgetWindows.CaptureOpenWidgets().ToList()
            };

            sessionService.Save(state);
        }
        catch
        {
            // Сбой сохранения сессии не должен мешать закрытию приложения.
        }

        // Состояние окна (позиция, размер, maximized)
        try
        {
            if (sender is not Window window) return;

            // RestoreBounds — это «нормальные» Left/Top/Width/Height
            // даже если окно сейчас Maximized. Если пусто (например окно
            // ни разу не показывалось нормально) — fallback на текущие свойства.
            var bounds = window.RestoreBounds;
            if (bounds.IsEmpty)
                bounds = new Rect(window.Left, window.Top, window.Width, window.Height);

            var windowStateService = Services.GetRequiredService<IWindowStateService>();
            var mainVm = Services.GetRequiredService<MainViewModel>();
            windowStateService.Save(new MainWindowState
            {
                Left = bounds.Left,
                Top = bounds.Top,
                Width = bounds.Width,
                Height = bounds.Height,
                IsMaximized = window.WindowState == System.Windows.WindowState.Maximized,
                IsHistoryVisible = mainVm.IsHistoryVisible
            });
        }
        catch
        {
            // Сбой сохранения позиции не должен мешать закрытию.
        }
    }

    /// <summary>
    /// Регистрация всех сервисов и ViewModel в DI-контейнере.
    /// Тот же подход что в ASP.NET Core (services.AddSingleton/AddTransient).
    /// </summary>
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Stateless сервисы — Singleton (нет состояния, безопасно делить).
        services.AddSingleton<ICalculatorService, CalculatorService>();
        services.AddSingleton<IConversionService, ConversionService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IThemeService, ThemeService>();

        // Файловые сервисы — Singleton с явным путём.
        // Лямбда (sp => ...) позволяет передать аргументы в конструктор.
        services.AddSingleton<ISettingsService>(_ => new SettingsService(AppPaths.SettingsFile));
        services.AddSingleton<IWidgetService>(_ => new WidgetService(AppPaths.WidgetsFile));
        services.AddSingleton<ISessionService>(_ => new SessionService(AppPaths.SessionFile));
        services.AddSingleton<IWindowStateService>(_ => new WindowStateService(AppPaths.WindowStateFile));

        // Сервисы открытия окон. Берут IServiceProvider и лениво резолвят
        // зависимости — это разрывает циклы DI.
        services.AddSingleton<IWidgetWindowService, WidgetWindowService>();
        services.AddSingleton<ISettingsWindowService, SettingsWindowService>();
        services.AddSingleton<ICreateWidgetWindowService, CreateWidgetWindowService>();
        services.AddSingleton<IAddWidgetWindowService, AddWidgetWindowService>();

        // ViewModels — Singleton для главной (одно окно — одна VM).
        services.AddSingleton<MainViewModel>();

        return services.BuildServiceProvider();
    }
}
