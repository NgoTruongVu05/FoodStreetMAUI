using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoodStreetGuide.Models;
using FoodStreetGuide.Services;

namespace FoodStreetGuide.ViewModels;

/// <summary>
/// ViewModel cho audio player: play/pause, progress, waveform animation.
/// </summary>
public sealed partial class AudioPlayerViewModel : BaseViewModel
{
    private readonly IAudioService _audio;

    public AudioPlayerViewModel(IAudioService audio)
    {
        _audio = audio;
        _audio.ProgressChanged  += OnProgressChanged;
        _audio.TrackStarted     += OnTrackStarted;
        _audio.TrackFinished    += OnTrackFinished;
    }

    // ── Properties ────────────────────────────────────────────────────────────
    [ObservableProperty] private string _trackTitle       = "—";
    [ObservableProperty] private string _trackDescription = string.Empty;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private string _positionText   = "0:00";
    [ObservableProperty] private string _durationText   = "0:00";
    [ObservableProperty] private bool   _isPlaying;
    [ObservableProperty] private bool   _hasTracks;
    [ObservableProperty] private string _sourceLabel    = "Studio";
    [ObservableProperty] private bool   _ttsEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayPauseIcon))]
    private PlaybackState _playbackState = PlaybackState.Idle;

    public string PlayPauseIcon => IsPlaying ? "⏸" : "▶";

    // ── Commands ──────────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task TogglePlayPauseAsync()
    {
        if (_audio.IsPlaying) await _audio.PauseAsync();
        else                  await _audio.ResumeAsync();
        IsPlaying = _audio.IsPlaying;
    }

    [RelayCommand]
    private async Task StopAsync() => await _audio.StopAsync();

    [RelayCommand]
    private async Task SeekAsync(double percent) => await _audio.SeekAsync(percent);

    [RelayCommand]
    private void ToggleTts()
    {
        TtsEnabled  = !TtsEnabled;
        SourceLabel = TtsEnabled ? "TTS" : "Studio";
    }

    // ── Event Handlers ────────────────────────────────────────────────────────
    private void OnTrackStarted(object? sender, AudioTrack track)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            TrackTitle       = track.Title;
            PlaybackState    = PlaybackState.Playing;
            IsPlaying        = true;
            HasTracks        = true;
            SourceLabel      = track.Source == AudioSource.TextToSpeech ? "TTS" : "Studio";
            ProgressPercent  = 0;
        });
    }

    private void OnTrackFinished(object? sender, AudioTrack track)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            PlaybackState   = PlaybackState.Finished;
            IsPlaying       = false;
            ProgressPercent = 100;
        });
    }

    private void OnProgressChanged(object? sender, double percent)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ProgressPercent = percent;
            var track = _audio.CurrentTrack;
            if (track is not null)
            {
                PositionText = FormatTime(track.Position);
                DurationText = FormatTime(track.Duration);
            }
        });
    }

    public void LoadTrack(PointOfInterest poi, AppLanguage language)
    {
        var content      = poi.Content.GetOrDefault(language);
        TrackTitle       = content.Title;
        TrackDescription = content.Description;
        HasTracks        = true;
        ProgressPercent  = 0;
        IsPlaying        = false;
        PlaybackState    = PlaybackState.Idle;
    }

    private static string FormatTime(TimeSpan t) =>
        t.TotalHours >= 1
            ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}"
            : $"{t.Minutes}:{t.Seconds:D2}";
}
