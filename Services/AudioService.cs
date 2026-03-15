using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FoodStreetMAUI.Models;

namespace FoodStreetMAUI.Services
{
    public class AudioService : IDisposable
    {
        public event EventHandler<string>? StatusChanged;

        public bool IsPlaying { get; private set; }
        public bool IsMuted { get; set; } = false;
        public float Volume { get; set; } = 1.0f;

        private CancellationTokenSource _cts = new();
        private readonly Queue<Func<Task>> _queue = new();
        private bool _processingQueue = false;

        public void PlayContent(LocalizedContent content, bool priority = false)
        {
            if (IsMuted) return;
            if (priority) StopAll();
            if (content.ContentType == ContentType.AudioFile
                && !string.IsNullOrEmpty(content.AudioFilePath))
                EnqueueAudioFile(content.AudioFilePath);
            else
                EnqueueTts(content.Description, content.Language);
        }

        public void StopAll()
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            _queue.Clear();
            IsPlaying = false;
            StatusChanged?.Invoke(this, "Đã dừng");
        }

        private void EnqueueTts(string text, string lang)
        {
            _queue.Enqueue(() => SpeakAsync(text, lang));
            if (!_processingQueue) _ = ProcessQueueAsync();
        }

        private void EnqueueAudioFile(string path)
        {
            _queue.Enqueue(() => PlayFileAsync(path));
            if (!_processingQueue) _ = ProcessQueueAsync();
        }

        private async Task ProcessQueueAsync()
        {
            _processingQueue = true;
            while (_queue.Count > 0)
                await _queue.Dequeue()();
            _processingQueue = false;
        }

        private async Task SpeakAsync(string text, string lang)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            IsPlaying = true;
            StatusChanged?.Invoke(this, text.Length > 40 ? text[..40] + "..." : text);
            try
            {
                var settings = new SpeechOptions
                {
                    Volume = Volume,
                    Pitch = 1.0f,
                    Locale = await GetLocaleAsync(lang),
                };
                await TextToSpeech.Default.SpeakAsync(text, settings, _cts.Token);
            }
            catch (TaskCanceledException) { }
            catch (Exception ex) { StatusChanged?.Invoke(this, "TTS loi: " + ex.Message); }
            finally { IsPlaying = false; StatusChanged?.Invoke(this, "Phat xong"); }
        }

        private static async Task<Locale?> GetLocaleAsync(string lang)
        {
            try
            {
                foreach (var l in await TextToSpeech.Default.GetLocalesAsync())
                {
                    var lc = l.Language.ToLower();
                    if (lang == "vi" && lc.StartsWith("vi")) return l;
                    if (lang == "en" && lc.StartsWith("en")) return l;
                    if (lang == "zh" && lc.StartsWith("zh")) return l;
                    if (lang == "ja" && lc.StartsWith("ja")) return l;
                    if (lang == "ko" && lc.StartsWith("ko")) return l;
                    if (lang == "fr" && lc.StartsWith("fr")) return l;
                }
            }
            catch { }
            return null;
        }

        private async Task PlayFileAsync(string path)
        {
            if (!File.Exists(path))
            {
                StatusChanged?.Invoke(this, "File khong ton tai: " + Path.GetFileName(path));
                return;
            }
            IsPlaying = true;
            StatusChanged?.Invoke(this, "Dang phat: " + Path.GetFileName(path));
            try { await Task.Delay(3000, _cts.Token); }
            catch (TaskCanceledException) { }
            finally { IsPlaying = false; StatusChanged?.Invoke(this, "Phat xong"); }
        }

        public void Dispose() => StopAll();
    }
}
