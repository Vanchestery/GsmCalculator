using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GsmCalculator.ViewModels;

/// <summary>
/// Базовый класс для всех ViewModel.
/// Реализует INotifyPropertyChanged — это интерфейс, без которого WPF Binding
/// не сможет узнать об изменениях свойств и UI не будет обновляться.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    /// <summary>
    /// Событие, на которое подписывается WPF. Когда мы его вызываем,
    /// WPF перечитывает значение свойства и обновляет UI.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Хелпер, который вызывает PropertyChanged.
    /// [CallerMemberName] автоматически подставляет имя свойства,
    /// откуда вызвали метод — не нужно писать nameof() вручную.
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Удобный метод для setter'ов свойств:
    ///   private string _name = "";
    ///   public string Name
    ///   {
    ///       get => _name;
    ///       set => SetProperty(ref _name, value);
    ///   }
    /// Возвращает true, если значение действительно изменилось
    /// (полезно для дополнительной логики в setter'е).
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
