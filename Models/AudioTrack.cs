namespace FoodStreetGuide.Models;

/// <summary>Nguồn gốc của audio playback.</summary>
public enum AudioSource
{
    StudioFile,   // MP3 pre-recorded trong Resources/Raw
    TextToSpeech  // TTS runtime
}

/// <summary>Trạng thái phát audio.</summary>
public enum PlaybackState
{
    Idle,
    Loading,
    Playing,
    Paused,
    Finished,
    Error
}

/// <summary>Đại diện cho một track audio đang hoặc sẽ phát.</summary>
public sealed class AudioTrack
{
    public Guid   Id          { get; init; } = Guid.NewGuid();
    public string Title       { get; init; } = string.Empty;
    public string PoiId       { get; init; } = string.Empty;
    public AudioSource Source { get; init; }

    // Dùng cho StudioFile
    public string? FilePath   { get; init; }

    // Dùng cho TTS
    public string? SpeechText { get; init; }
    public string? Locale     { get; init; }  // "vi-VN", "en-US", "zh-CN", "ja-JP"

    public TimeSpan Duration  { get; set; }
    public TimeSpan Position  { get; set; }
    public PlaybackState State { get; set; } = PlaybackState.Idle;

    public double ProgressPercent =>
        Duration.TotalSeconds > 0
            ? Position.TotalSeconds / Duration.TotalSeconds * 100
            : 0;
}

/// <summary>Cấu hình GPS tracking – thay đổi theo foreground/background.</summary>
public sealed class GpsTrackingConfig
{
    public static GpsTrackingConfig Foreground => new()
    {
        Interval          = TimeSpan.FromSeconds(2),
        DesiredAccuracy   = 5,    // mét
        MinDistance       = 3,    // mét di chuyển tối thiểu mới update
        Mode              = GpsMode.Foreground
    };

    public static GpsTrackingConfig Background => new()
    {
        Interval          = TimeSpan.FromSeconds(10),
        DesiredAccuracy   = 20,
        MinDistance       = 10,
        Mode              = GpsMode.Background
    };

    public static GpsTrackingConfig PowerSave => new()
    {
        Interval          = TimeSpan.FromSeconds(30),
        DesiredAccuracy   = 50,
        MinDistance       = 25,
        Mode              = GpsMode.Background
    };

    public TimeSpan Interval        { get; init; }
    public double DesiredAccuracy   { get; init; }
    public double MinDistance       { get; init; }
    public GpsMode Mode             { get; init; }
}

public enum GpsMode { Foreground, Background }
