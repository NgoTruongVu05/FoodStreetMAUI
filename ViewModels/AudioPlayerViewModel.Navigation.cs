// Partial: AudioPlayerViewModel.Navigation.cs
using CommunityToolkit.Mvvm.Input;

namespace FoodStreetGuide.ViewModels;

public sealed partial class AudioPlayerViewModel
{
    [RelayCommand]
    private void SkipBack()
    {
        // Nếu progress > 5%, seek về đầu; nếu không, báo MainViewModel lùi POI
        if (ProgressPercent > 5)
            _ = SeekAsync(0);
        else
            SkipBackRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void SkipForward() =>
        SkipForwardRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>MainViewModel đăng ký để điều hướng POI khi skip.</summary>
    public event EventHandler? SkipBackRequested;
    public event EventHandler? SkipForwardRequested;
}
