using CommunityToolkit.Mvvm.ComponentModel;

namespace FoodStreetGuide.ViewModels;

/// <summary>
/// Base ViewModel: INotifyPropertyChanged qua CommunityToolkit.Mvvm source-gen.
/// Thêm IsBusy, ErrorMessage dùng chung toàn app.
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public bool IsNotBusy => !IsBusy;

    protected async Task RunSafeAsync(Func<Task> action, string? errorPrefix = null)
    {
        if (IsBusy) return;
        IsBusy      = true;
        ErrorMessage = null;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"{errorPrefix ?? "Lỗi"}: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[VM] {ErrorMessage}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
