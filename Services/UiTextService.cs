using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Diagnostics;
using FoodStreetMAUI.Configuration;

namespace FoodStreetMAUI.Services
{
    public sealed class MainPageUiTexts
    {
        public string HeaderTitle { get; set; } = "🍜 Food Street Guide";
        public string HeaderSubtitle { get; set; } = "Thuyết minh tự động";
        public string LanguageLabel { get; set; } = "Ngôn ngữ:";
        public string GpsIcon { get; set; } = "📍";
        public string GpsStatusNotStarted { get; set; } = "GPS chưa khởi động";
        public string GpsStartButton { get; set; } = "Bật GPS";
        public string GpsStopButton { get; set; } = "Tắt GPS";
        public string PoiListButton { get; set; } = "📌 Danh sách điểm";
        public string PoiListPageTitle { get; set; } = "Danh sách POI";
        public string LegendYou { get; set; } = "Bạn";
        public string LegendDestination { get; set; } = "Điểm đến";
        public string LegendApproaching { get; set; } = "Gần đến";
        public string LegendNearest { get; set; } = "Gần nhất";
        public string NowPlayingHeader { get; set; } = "🔊 Đang phát thuyết minh";
        public string StopAudioButton { get; set; } = "⏹ Dừng";
        public string AudioWaitingTitle { get; set; } = "Chờ kích hoạt POI...";
        public string PoiModalPlayButton { get; set; } = "🔊";
        public string PoiModalViewDetailButton { get; set; } = "Xem chi tiết";
        public string CommonClose { get; set; } = "Đóng";
        public string LanguageModalTitle { get; set; } = "Cài đặt ngôn ngữ";
        public string NarrationLanguageLabel { get; set; } = "Ngôn ngữ thuyết minh";
        public string SystemLanguageLabel { get; set; } = "Ngôn ngữ hệ thống";
        public string DropdownArrow { get; set; } = "▼";
        public string StatsVisitedLabel { get; set; } = "Điểm thăm";
        public string StatsDistanceLabel { get; set; } = "Quãng đường";
        public string StatsTimeLabel { get; set; } = "Thời gian";
        public string StatsVisitedFormat { get; set; } = "🏛️ {0}";
        public string PoiDetailTitle { get; set; } = "Chi tiết điểm";
        public string PoiDetailNoImage { get; set; } = "Chưa có ảnh";
        public string PoiDetailOpenMaps { get; set; } = "Mở trên Google Maps";
    }

    public sealed class QrScanPageUiTexts
    {
        public string ScanTitle { get; set; } = "Quét QR";
        public string ScanHint { get; set; } = "Đưa mã QR vào khung để quét";
        public string CloseButton { get; set; } = "Đóng";
        public string CameraPermissionTitle { get; set; } = "Quyền camera";
        public string CameraPermissionMessage { get; set; } = "Bạn cần cấp quyền camera để quét QR.";
    }

    public class UiTextService
    {
        private readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

#if DEBUG
        private static void TraceLog(string message)
        {
            try
            {
                Debug.WriteLine($"[UiTextService] {message}");
                Console.WriteLine($"[UiTextService] {message}");
            }
            catch
            {
            }
        }
#else
        private static void TraceLog(string message) { }
#endif

        public async Task<MainPageUiTexts> LoadOrCreateMainPageTextsAsync(string languageCode)
        {
            var normalizedCode = NormalizeCode(languageCode);

            TraceLog($"LoadOrCreateMainPageTextsAsync: requested='{languageCode}', normalized='{normalizedCode}'");

            // 1) Package first (shipped translations)
            // 2) Local cache (previously generated/overridden)
            var root = await TryReadLanguageJsonFromPackageAsync(normalizedCode)
                       ?? await TryReadLanguageJsonFromLocalAsync(normalizedCode);

            TraceLog(root != null
                ? $"Loaded language JSON for '{normalizedCode}' (package/local)."
                : $"Language JSON for '{normalizedCode}' not found in package/local. Will try EnsureLanguageFileAsync.");

            // If not found in either place, then try generating it once.
            if (root == null)
            {
                TraceLog($"EnsureLanguageFileAsync: generating local language file for '{normalizedCode}'...");
                await EnsureLanguageFileAsync(normalizedCode);
                root = await TryReadLanguageJsonFromLocalAsync(normalizedCode)
                       ?? await TryReadLanguageJsonFromPackageAsync(normalizedCode);

                TraceLog(root != null
                    ? $"After EnsureLanguageFileAsync, loaded language JSON for '{normalizedCode}'."
                    : $"After EnsureLanguageFileAsync, still cannot load language JSON for '{normalizedCode}'.");
            }

            if (root == null)
            {
                TraceLog("Falling back to 'vi' language JSON.");
                root = await TryReadLanguageJsonFromPackageAsync("vi")
                       ?? await TryReadLanguageJsonFromLocalAsync("vi");
            }

            return MapMainPageTexts(root);
        }

        public async Task<QrScanPageUiTexts> LoadOrCreateQrScanPageTextsAsync(string languageCode)
        {
            var normalizedCode = NormalizeCode(languageCode);

            var root = await TryReadLanguageJsonFromPackageAsync(normalizedCode)
                       ?? await TryReadLanguageJsonFromLocalAsync(normalizedCode);

            if (root == null)
            {
                await EnsureLanguageFileAsync(normalizedCode);
                root = await TryReadLanguageJsonFromLocalAsync(normalizedCode)
                       ?? await TryReadLanguageJsonFromPackageAsync(normalizedCode);
            }

            if (root == null)
            {
                root = await TryReadLanguageJsonFromPackageAsync("vi")
                       ?? await TryReadLanguageJsonFromLocalAsync("vi");
            }

            return MapQrScanPageTexts(root);
        }

        public Task<MainPageUiTexts> LoadMainPageTextsAsync(string languageCode = "vi")
            => LoadOrCreateMainPageTextsAsync(languageCode);

        private async Task EnsureLanguageFileAsync(string languageCode)
        {
            if (languageCode == "vi") return;

            var source = await TryReadLanguageJsonFromPackageAsync("vi")
                         ?? await TryReadLanguageJsonFromLocalAsync("vi");
            if (source == null) return;

            JsonNode translated;
            try
            {
                var targetLanguage = NormalizeTargetLanguage(languageCode);
                translated = await TranslateNodeAsync(source.DeepClone(), "vi", targetLanguage);
            }
            catch
            {
                return;
            }

            if (translated is JsonObject obj)
            {
                obj["meta"] ??= new JsonObject();
                if (obj["meta"] is JsonObject meta)
                {
                    meta["languageCode"] = languageCode;
                }
            }

            await SaveLanguageJsonAsync(languageCode, translated);
        }

        private async Task<JsonNode?> TryReadLanguageJsonFromLocalAsync(string languageCode)
        {
            var normalizedCode = NormalizeCode(languageCode);
            var fallbackCode = NormalizeTargetLanguage(normalizedCode);

            var localPath = Path.Combine(FileSystem.AppDataDirectory, "Languages", $"{normalizedCode}.json");
            TraceLog($"TryReadLocal: '{normalizedCode}' -> {localPath}");
            if (File.Exists(localPath))
            {
                TraceLog($"TryReadLocal: FOUND {localPath}");
                var localJson = await File.ReadAllTextAsync(localPath);
                return JsonNode.Parse(localJson);
            }

            if (!string.Equals(fallbackCode, normalizedCode, StringComparison.OrdinalIgnoreCase))
            {
                var fallbackLocalPath = Path.Combine(FileSystem.AppDataDirectory, "Languages", $"{fallbackCode}.json");
                TraceLog($"TryReadLocal: fallback '{fallbackCode}' -> {fallbackLocalPath}");
                if (File.Exists(fallbackLocalPath))
                {
                    TraceLog($"TryReadLocal: FOUND {fallbackLocalPath}");
                    var localJson = await File.ReadAllTextAsync(fallbackLocalPath);
                    return JsonNode.Parse(localJson);
                }
            }

            TraceLog($"TryReadLocal: NOT FOUND for '{normalizedCode}'");
            return null;
        }

        private async Task<JsonNode?> TryReadLanguageJsonFromPackageAsync(string languageCode)
        {
            var normalizedCode = NormalizeCode(languageCode);
            var fallbackCode = NormalizeTargetLanguage(normalizedCode);
            var lowerVariant = normalizedCode.ToLowerInvariant();

            TraceLog($"TryReadPackage: requested='{languageCode}', normalized='{normalizedCode}', lower='{lowerVariant}', fallback='{fallbackCode}'");

            try
            {
                var path = $"Languages/{normalizedCode}.json";
                TraceLog($"TryReadPackage: trying {path}");
                await using var stream = await FileSystem.OpenAppPackageFileAsync(path);
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                TraceLog($"TryReadPackage: SUCCESS {path}");
                return JsonNode.Parse(json);
            }
            catch
            {
                // In case the asset was packaged with a different logical path.
                try
                {
                    var path = $"Resources/Languages/{normalizedCode}.json";
                    TraceLog($"TryReadPackage: trying {path}");
                    await using var stream = await FileSystem.OpenAppPackageFileAsync(path);
                    using var reader = new StreamReader(stream);
                    var json = await reader.ReadToEndAsync();
                    TraceLog($"TryReadPackage: SUCCESS {path}");
                    return JsonNode.Parse(json);
                }
                catch
                {
                }

                // Some platforms/build pipelines may lowercase embedded asset names.
                if (!string.Equals(lowerVariant, normalizedCode, StringComparison.Ordinal))
                {
                    try
                    {
                        var path = $"Languages/{lowerVariant}.json";
                        TraceLog($"TryReadPackage: trying {path}");
                        await using var stream = await FileSystem.OpenAppPackageFileAsync(path);
                        using var reader = new StreamReader(stream);
                        var json = await reader.ReadToEndAsync();
                        TraceLog($"TryReadPackage: SUCCESS {path}");
                        return JsonNode.Parse(json);
                    }
                    catch
                    {
                    }
                }

                if (!string.Equals(fallbackCode, normalizedCode, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var path = $"Languages/{fallbackCode}.json";
                        TraceLog($"TryReadPackage: trying {path}");
                        await using var stream = await FileSystem.OpenAppPackageFileAsync(path);
                        using var reader = new StreamReader(stream);
                        var json = await reader.ReadToEndAsync();
                        TraceLog($"TryReadPackage: SUCCESS {path}");
                        return JsonNode.Parse(json);
                    }
                    catch
                    {
                    }
                }

                TraceLog($"TryReadPackage: NOT FOUND for '{normalizedCode}'");
                return null;
            }
        }

        private static MainPageUiTexts MapMainPageTexts(JsonNode? root)
        {
            var texts = new MainPageUiTexts();
            if (root == null) return texts;

            texts.HeaderTitle = Get(root, "mainPage.header.title", texts.HeaderTitle);
            texts.HeaderSubtitle = Get(root, "mainPage.header.subtitle", texts.HeaderSubtitle);
            texts.LanguageLabel = Get(root, "mainPage.language.label", texts.LanguageLabel);
            texts.GpsIcon = Get(root, "mainPage.gps.icon", texts.GpsIcon);
            texts.GpsStatusNotStarted = Get(root, "mainPage.gps.statusNotStarted", texts.GpsStatusNotStarted);
            texts.GpsStartButton = Get(root, "mainPage.gps.startButton", texts.GpsStartButton);
            texts.GpsStopButton = Get(root, "mainPage.gps.stopButton", texts.GpsStopButton);
            texts.PoiListButton = Get(root, "mainPage.map.poiListButton", texts.PoiListButton);
            texts.PoiListPageTitle = Get(root, "mainPage.map.poiListPageTitle", texts.PoiListPageTitle);
            texts.LegendYou = Get(root, "mainPage.legend.you", texts.LegendYou);
            texts.LegendDestination = Get(root, "mainPage.legend.destination", texts.LegendDestination);
            texts.LegendApproaching = Get(root, "mainPage.legend.approaching", texts.LegendApproaching);
            texts.LegendNearest = Get(root, "mainPage.legend.nearest", texts.LegendNearest);
            texts.NowPlayingHeader = Get(root, "mainPage.audio.nowPlayingHeader", texts.NowPlayingHeader);
            texts.StopAudioButton = Get(root, "mainPage.audio.stopButton", texts.StopAudioButton);
            texts.AudioWaitingTitle = Get(root, "mainPage.audio.waitingTitle", texts.AudioWaitingTitle);
            texts.PoiModalPlayButton = Get(root, "mainPage.poiModal.playButton", texts.PoiModalPlayButton);
            texts.PoiModalViewDetailButton = Get(root, "mainPage.poiModal.viewDetailButton", texts.PoiModalViewDetailButton);
            texts.CommonClose = Get(root, "mainPage.common.close", texts.CommonClose);
            texts.LanguageModalTitle = Get(root, "mainPage.language.modalTitle", texts.LanguageModalTitle);
            texts.NarrationLanguageLabel = Get(root, "mainPage.language.narrationLanguageLabel", texts.NarrationLanguageLabel);
            texts.SystemLanguageLabel = Get(root, "mainPage.language.systemLanguageLabel", texts.SystemLanguageLabel);
            texts.DropdownArrow = Get(root, "mainPage.common.dropdownArrow", texts.DropdownArrow);
            texts.StatsVisitedLabel = Get(root, "mainPage.stats.visited", texts.StatsVisitedLabel);
            texts.StatsDistanceLabel = Get(root, "mainPage.stats.distance", texts.StatsDistanceLabel);
            texts.StatsTimeLabel = Get(root, "mainPage.stats.time", texts.StatsTimeLabel);
            texts.StatsVisitedFormat = Get(root, "mainPage.stats.visitedFormat", texts.StatsVisitedFormat);
            texts.PoiDetailTitle = Get(root, "poiDetailPage.title", texts.PoiDetailTitle);
            texts.PoiDetailNoImage = Get(root, "poiDetailPage.noImage", texts.PoiDetailNoImage);
            texts.PoiDetailOpenMaps = Get(root, "poiDetailPage.openMaps", texts.PoiDetailOpenMaps);

            return texts;
        }

        private static QrScanPageUiTexts MapQrScanPageTexts(JsonNode? root)
        {
            var texts = new QrScanPageUiTexts();
            if (root == null) return texts;

            texts.ScanTitle = Get(root, "qr.scanTitle", texts.ScanTitle);
            texts.ScanHint = Get(root, "qr.scanHint", texts.ScanHint);
            texts.CloseButton = Get(root, "qr.closeButton", texts.CloseButton);
            texts.CameraPermissionTitle = Get(root, "qr.cameraPermissionTitle", texts.CameraPermissionTitle);
            texts.CameraPermissionMessage = Get(root, "qr.cameraPermissionMessage", texts.CameraPermissionMessage);

            return texts;
        }

        private async Task<JsonNode> TranslateNodeAsync(JsonNode node, string sourceLanguage, string targetLanguage)
        {
            if (node is JsonObject obj)
            {
                foreach (var key in obj.Select(k => k.Key).ToList())
                {
                    var child = obj[key];
                    if (child != null)
                    {
                        obj[key] = await TranslateNodeAsync(child, sourceLanguage, targetLanguage);
                    }
                }
                return obj;
            }

            if (node is JsonArray arr)
            {
                for (var i = 0; i < arr.Count; i++)
                {
                    if (arr[i] != null)
                    {
                        arr[i] = await TranslateNodeAsync(arr[i]!, sourceLanguage, targetLanguage);
                    }
                }
                return arr;
            }

            if (node is JsonValue value && value.TryGetValue<string>(out var textValue))
            {
                var translated = await TranslateTextAsync(textValue, sourceLanguage, targetLanguage);
                return JsonValue.Create(translated) ?? JsonValue.Create(textValue)!;
            }

            return node;
        }

        private async Task<string> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            if (string.IsNullOrWhiteSpace(LangblySecrets.BaseUrl) || string.IsNullOrWhiteSpace(LangblySecrets.ApiKey))
            {
                throw new InvalidOperationException("Langbly API is not configured.");
            }

            var endpointCandidates = BuildEndpointCandidates(LangblySecrets.BaseUrl);
            foreach (var endpoint in endpointCandidates)
            {
                var translated = await TryTranslateByEndpointAsync(endpoint, text, sourceLanguage, targetLanguage);
                if (!string.IsNullOrWhiteSpace(translated))
                {
                    return translated;
                }
            }

            throw new InvalidOperationException("Langbly translation failed.");
        }

        private async Task<string?> TryTranslateByEndpointAsync(string endpoint, string text, string sourceLanguage, string targetLanguage)
        {
            var payloadCandidates = new object[]
            {
                new { text, source = sourceLanguage, target = targetLanguage },
                new { q = text, source = sourceLanguage, target = targetLanguage, format = "text" },
                new { text, source_language = sourceLanguage, target_language = targetLanguage }
            };

            foreach (var payload in payloadCandidates)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", LangblySecrets.ApiKey);
                    request.Headers.Add("x-api-key", LangblySecrets.ApiKey);
                    request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                    using var response = await _httpClient.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        continue;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var root = JsonNode.Parse(content);
                    var translated = ExtractTranslatedText(root);
                    if (!string.IsNullOrWhiteSpace(translated))
                    {
                        return translated;
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private static string? ExtractTranslatedText(JsonNode? root)
        {
            return root?["translatedText"]?.GetValue<string>()
                   ?? root?["translation"]?.GetValue<string>()
                   ?? root?["data"]?["translatedText"]?.GetValue<string>()
                   ?? root?["data"]?["translation"]?.GetValue<string>()
                   ?? root?["result"]?["translatedText"]?.GetValue<string>()
                   ?? root?["result"]?["text"]?.GetValue<string>()
                   ?? root?["data"]?["translations"]?[0]?["translatedText"]?.GetValue<string>()
                   ?? root?["translations"]?[0]?["translatedText"]?.GetValue<string>();
        }

        private static List<string> BuildEndpointCandidates(string baseUrl)
        {
            var normalized = baseUrl.TrimEnd('/');
            var candidates = new List<string> { normalized };

            if (!normalized.EndsWith("/translate", StringComparison.OrdinalIgnoreCase)
                && !normalized.EndsWith("/translate/v2", StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add(normalized + "/translate");
                candidates.Add(normalized + "/translate/v2");
            }

            return candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string NormalizeTargetLanguage(string? languageCode)
        {
            var code = NormalizeCode(languageCode);
            var idx = code.IndexOf('-');
            return idx > 0 ? code[..idx].ToLowerInvariant() : code.ToLowerInvariant();
        }

        private static async Task SaveLanguageJsonAsync(string languageCode, JsonNode root)
        {
            var dir = Path.Combine(FileSystem.AppDataDirectory, "Languages");
            Directory.CreateDirectory(dir);
            var filePath = Path.Combine(dir, $"{NormalizeCode(languageCode)}.json");

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };

            await File.WriteAllTextAsync(filePath, root.ToJsonString(options));
        }

        private static string NormalizeCode(string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return "vi";
            }

            var code = languageCode.Trim();

            // Keep region casing for package file matching (e.g. `zh-CN.json`),
            // but normalize language part to lower-case.
            var parts = code.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                return parts[0].ToLowerInvariant();
            }

            var lang = parts[0].ToLowerInvariant();
            var region = parts[1].ToUpperInvariant();
            return $"{lang}-{region}";
        }

        private static string Get(JsonNode root, string path, string fallback)
        {
            JsonNode? current = root;
            foreach (var segment in path.Split('.'))
            {
                current = current?[segment];
                if (current == null) return fallback;
            }

            return current is JsonValue v && v.TryGetValue<string>(out var value) ? value : fallback;
        }
    }
}
