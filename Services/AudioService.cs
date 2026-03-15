using FoodStreetGuide.Models;
using Plugin.Maui.Audio;

namespace FoodStreetGuide.Services;

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IAudioService : IAsyncDisposable
{
    AudioTrack? CurrentTrack { get; }
    bool        IsPlaying    { get; }

    event EventHandler<AudioTrack>? TrackStarted;
    event EventHandler<AudioTrack>? TrackFinished;
    event EventHandler<double>?     ProgressChanged; // 0–100

    Task PlayStudioFileAsync(AudioTrack track);
    Task PlayTtsAsync(AudioTrack track);
    Task PauseAsync();
    Task ResumeAsync();
    Task StopAsync();
    Task SeekAsync(double percent);
    void SetVolume(float volume);  // 0.0–1.0
}

// ── Implementation ────────────────────────────────────────────────────────────

/// <summary>
/// Audio service hai nguồn: Studio MP3 (Plugin.Maui.Audio) và TTS (MAUI TextToSpeech).
/// Dừng track cũ tự động khi có track mới.
/// </summary>
public sealed class AudioService : IAudioService
{
    private readonly IAudioManager _audioManager;
    private IAudioPlayer?         _player;
    private CancellationTokenSource? _progressCts;
    private float _volume = 1.0f;

    public AudioTrack? CurrentTrack { get; private set; }
    public bool IsPlaying => _player?.IsPlaying ?? false;

    public event EventHandler<AudioTrack>? TrackStarted;
    public event EventHandler<AudioTrack>? TrackFinished;
    public event EventHandler<double>?     ProgressChanged;

    public AudioService(IAudioManager audioManager)
    {
        _audioManager = audioManager;
    }

    // ── Studio File ───────────────────────────────────────────────────────────
    public async Task PlayStudioFileAsync(AudioTrack track)
    {
        await StopAsync();

        CurrentTrack       = track with { State = PlaybackState.Loading, Source = AudioSource.StudioFile };
        TrackStarted?.Invoke(this, CurrentTrack);

        try
        {
            // Mở file từ bundle
            await using var stream = await FileSystem.OpenAppPackageFileAsync(track.FilePath!);
            _player = _audioManager.CreatePlayer(stream);
            _player.Volume = _volume;
            _player.PlaybackEnded += OnPlaybackEnded;

            _player.Play();
            CurrentTrack = CurrentTrack with
            {
                State    = PlaybackState.Playing,
                Duration = TimeSpan.FromSeconds(_player.Duration)
            };
            StartProgressPolling();
        }
        catch (Exception ex)
        {
            CurrentTrack = CurrentTrack with { State = PlaybackState.Error };
            System.Diagnostics.Debug.WriteLine($"[Audio] PlayStudioFile error: {ex.Message}");
        }
    }

    // ── TTS ───────────────────────────────────────────────────────────────────
    public async Task PlayTtsAsync(AudioTrack track)
    {
        await StopAsync();
        CurrentTrack = track with { State = PlaybackState.Loading, Source = AudioSource.TextToSpeech };
        TrackStarted?.Invoke(this, CurrentTrack);

        try
        {
            var options = new SpeechOptions
            {
                Locale = await GetLocaleAsync(track.Locale),
                Volume = _volume,
                Pitch  = 1.0f
            };

            CurrentTrack = CurrentTrack with { State = PlaybackState.Playing };
            await TextToSpeech.SpeakAsync(track.SpeechText ?? string.Empty, options);
            OnTrackComplete();
        }
        catch (Exception ex)
        {
            CurrentTrack = CurrentTrack with { State = PlaybackState.Error };
            System.Diagnostics.Debug.WriteLine($"[Audio] TTS error: {ex.Message}");
        }
    }

    // ── Controls ──────────────────────────────────────────────────────────────
    public Task PauseAsync()
    {
        _player?.Pause();
        if (CurrentTrack is not null)
            CurrentTrack = CurrentTrack with { State = PlaybackState.Paused };
        _progressCts?.Cancel();
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        _player?.Play();
        if (CurrentTrack is not null)
        {
            CurrentTrack = CurrentTrack with { State = PlaybackState.Playing };
            StartProgressPolling();
        }
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _progressCts?.Cancel();
        if (_player is not null)
        {
            _player.PlaybackEnded -= OnPlaybackEnded;
            _player.Stop();
            _player.Dispose();
            _player = null;
        }
        if (CurrentTrack is not null)
            CurrentTrack = CurrentTrack with { State = PlaybackState.Idle };
        return Task.CompletedTask;
    }

    public Task SeekAsync(double percent)
    {
        if (_player is null) return Task.CompletedTask;
        var secs = _player.Duration * (percent / 100.0);
        _player.Seek(secs);
        return Task.CompletedTask;
    }

    public void SetVolume(float volume)
    {
        _volume = Math.Clamp(volume, 0f, 1f);
        if (_player is not null) _player.Volume = _volume;
    }

    // ── Internal helpers ──────────────────────────────────────────────────────
    private void StartProgressPolling()
    {
        _progressCts?.Cancel();
        _progressCts = new CancellationTokenSource();
        var token = _progressCts.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested && (_player?.IsPlaying ?? false))
            {
                if (_player is not null && _player.Duration > 0)
                {
                    double pct = _player.CurrentPosition / _player.Duration * 100;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (CurrentTrack is not null)
                            CurrentTrack = CurrentTrack with
                            {
                                Position = TimeSpan.FromSeconds(_player.CurrentPosition)
                            };
                        ProgressChanged?.Invoke(this, pct);
                    });
                }
                await Task.Delay(500, token).ConfigureAwait(false);
            }
        }, token);
    }

    private void OnPlaybackEnded(object? sender, EventArgs e) => OnTrackComplete();

    private void OnTrackComplete()
    {
        _progressCts?.Cancel();
        if (CurrentTrack is not null)
        {
            CurrentTrack = CurrentTrack with { State = PlaybackState.Finished };
            TrackFinished?.Invoke(this, CurrentTrack);
        }
    }

    private static async Task<Locale?> GetLocaleAsync(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var locales = await TextToSpeech.GetLocalesAsync();
        return locales.FirstOrDefault(l =>
            l.Language.StartsWith(code.Split('-')[0], StringComparison.OrdinalIgnoreCase));
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _progressCts?.Dispose();
    }
}
