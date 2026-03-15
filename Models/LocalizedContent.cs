namespace FoodStreetGuide.Models;

/// <summary>Ngôn ngữ thuyết minh được hỗ trợ.</summary>
public enum AppLanguage
{
    Vietnamese = 0,
    English    = 1,
    Chinese    = 2,
    Japanese   = 3
}

/// <summary>
/// Nội dung thuyết minh đa ngôn ngữ cho một POI.
/// </summary>
public sealed class LocalizedContent
{
    public string Title       { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string AudioFile   { get; init; } = string.Empty;   // tên file trong Resources/Raw
    public AppLanguage Language { get; init; }

    public bool HasAudioFile => !string.IsNullOrWhiteSpace(AudioFile);
}

/// <summary>
/// Bản đồ nội dung theo từng ngôn ngữ, tra cứu O(1).
/// </summary>
public sealed class LocalizedContentMap
{
    private readonly Dictionary<AppLanguage, LocalizedContent> _map = new();

    public LocalizedContentMap Add(LocalizedContent content)
    {
        _map[content.Language] = content;
        return this;
    }

    public LocalizedContent GetOrDefault(AppLanguage language)
    {
        if (_map.TryGetValue(language, out var content)) return content;
        // fallback: Vietnamese → English → first available
        if (_map.TryGetValue(AppLanguage.Vietnamese, out content)) return content;
        if (_map.TryGetValue(AppLanguage.English,    out content)) return content;
        return _map.Values.FirstOrDefault() ?? new LocalizedContent();
    }

    public IReadOnlyCollection<AppLanguage> SupportedLanguages =>
        _map.Keys.ToList().AsReadOnly();
}
