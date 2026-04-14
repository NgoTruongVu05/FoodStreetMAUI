using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FoodStreetMAUI.Configuration;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace FoodStreetMAUI.Services
{
    public sealed class HeartbeatService : IDisposable
    {
        private const string ClientIdPreferenceKey = "heartbeat_client_id";
        private const string Actor = "tourist";
        private const string AppName = "poi-mobile";

        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private CancellationTokenSource? _cts;
        private bool _isRunning;

        public int IntervalSeconds { get; set; } = 30;

        public HeartbeatService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            _clientId = Preferences.Get(ClientIdPreferenceKey, string.Empty);
            if (string.IsNullOrWhiteSpace(_clientId))
            {
                _clientId = Guid.NewGuid().ToString("N");
                Preferences.Set(ClientIdPreferenceKey, _clientId);
            }
        }

        public void Start()
        {
            if (_isRunning)
            {
                return;
            }

            _isRunning = true;
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => RunAsync(_cts.Token));
        }

        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async Task RunAsync(CancellationToken ct)
        {
            await SendHeartbeatAsync(ct);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(IntervalSeconds), ct);
                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }

                    await SendHeartbeatAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Heartbeat loop error: {ex.Message}");
                }
            }
        }

        private async Task SendHeartbeatAsync(CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;
            var payload = new[]
            {
                new
                {
                    client_id = _clientId,
                    actor = Actor,
                    app = AppName,
                    platform = DeviceInfo.Platform.ToString(),
                    last_seen = now,
                    updated_at = now
                }
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{SupabaseSecrets.Url}/rest/v1/app_heartbeats?on_conflict=client_id");

            request.Headers.Add("apikey", SupabaseSecrets.ApiKey);
            request.Headers.Add("Authorization", $"Bearer {SupabaseSecrets.ApiKey}");
            request.Headers.Add("Prefer", "resolution=merge-duplicates,return=minimal");
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                System.Diagnostics.Debug.WriteLine($"Heartbeat failed {(int)response.StatusCode}: {error}");
            }
        }

        public void Dispose()
        {
            Stop();
            _httpClient.Dispose();
        }
    }
}
