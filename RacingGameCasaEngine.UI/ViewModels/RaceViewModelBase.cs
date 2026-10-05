using System.Runtime.CompilerServices;
using MGUI.Shared.Helpers;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>Base of the screen view models: a property notifies only when its value changes (CasaEngine ADR-0038).</summary>
public abstract class RaceViewModelBase : ViewModelBase
{
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
}
