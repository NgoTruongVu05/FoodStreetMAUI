using System;
using System.Collections.Generic;
using System.Net.Http;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using FoodStreetMAUI.Configuration;
using SQLite;

namespace FoodStreetMAUI.Services
{
    public sealed class LanguageDto
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }

    public class LanguageService
    {
        private readonly HttpClient _httpClient;
        private const string SupabaseUrl = SupabaseSecrets.Url;
        private const string SupabaseApiKey = SupabaseSecrets.ApiKey;
        private static string LanguageDbPath =>
            Path.Combine(FileSystem.AppDataDirectory, "languages.db3");

        private SQLiteAsyncConnection? _db;

        public LanguageService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        private async Task InitDbAsync()
        {
            if (_db != null) return;
            _db = new SQLiteAsyncConnection(LanguageDbPath);
            await _db.CreateTableAsync<LanguageEntity>();
        }

        private async Task<List<LanguageDto>> LoadCachedLanguagesAsync()
        {
            await InitDbAsync();
            var entities = await _db.Table<LanguageEntity>().ToListAsync();
            return entities.Select(e => new LanguageDto
            {
                Code = e.Code ?? string.Empty,
                Name = e.Name ?? string.Empty
            }).ToList();
        }

        private async Task SaveLanguagesAsync(List<LanguageDto> languages)
        {
            await InitDbAsync();
            await _db.DeleteAllAsync<LanguageEntity>();
            var entities = languages.Select(l => new LanguageEntity
            {
                Code = l.Code ?? string.Empty,
                Name = l.Name ?? string.Empty
            }).ToList();
            await _db.InsertAllAsync(entities);
        }

        public async Task<List<LanguageDto>> GetLanguagesAsync()
        {
            try
            {
                _httpClient.Timeout = TimeSpan.FromSeconds(15);
                var request = new HttpRequestMessage(HttpMethod.Get, $"{SupabaseUrl}/rest/v1/languages?select=code,name");
                request.Headers.Add("apikey", SupabaseApiKey);
                request.Headers.Add("Authorization", $"Bearer {SupabaseApiKey}");
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<List<LanguageDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result != null && result.Count > 0)
                    {
                        await SaveLanguagesAsync(result);
                        return result;
                    }

                    return await LoadCachedLanguagesAsync();
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Supabase languages response {(int)response.StatusCode} {response.ReasonPhrase}: {errorContent}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching languages: {ex.Message}");
            }

            try
            {
                return await LoadCachedLanguagesAsync();
            }
            catch
            {
                return new List<LanguageDto>();
            }
        }

        private sealed class LanguageEntity
        {
            [PrimaryKey]
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }
    }
}
