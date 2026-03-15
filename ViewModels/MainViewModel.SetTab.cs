// File bổ sung: ViewModels/MainViewModel.SetTab.cs
// Partial extension để tách RelayCommands khỏi file chính

using CommunityToolkit.Mvvm.Input;
using FoodStreetGuide.Models;

namespace FoodStreetGuide.ViewModels;

public sealed partial class MainViewModel
{
    [RelayCommand]
    private void SetTab(string indexStr)
    {
        if (int.TryParse(indexStr, out int idx))
            ActiveTabIndex = idx;
    }

    /// <summary>
    /// Gọi khi user bấm Skip Back trong AudioPlayer.
    /// Chọn POI gần nhất đang Inside hoặc Nearby theo ưu tiên.
    /// </summary>
    [RelayCommand]
    private void SelectPreviousPoi()
    {
        var current = ActivePoi;
        if (current is null) return;
        int idx = Pois.IndexOf(current);
        if (idx > 0) SelectPoiCommand.Execute(Pois[idx - 1]);
    }

    [RelayCommand]
    private void SelectNextPoi()
    {
        var current = ActivePoi;
        if (current is null) return;
        int idx = Pois.IndexOf(current);
        if (idx < Pois.Count - 1) SelectPoiCommand.Execute(Pois[idx + 1]);
    }
}
