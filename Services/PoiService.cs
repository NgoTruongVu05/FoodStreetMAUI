using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FoodStreetMAUI.Configuration;

namespace FoodStreetMAUI.Services
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("data")]
        public T Data { get; set; }

        [JsonPropertyName("error")]
        public ApiError Error { get; set; }
    }

    public class ApiError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class PoiDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("categoryId")]
        public string CategoryId { get; set; }

        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; }

        [JsonPropertyName("categoryColor")]
        public string CategoryColor { get; set; }

        [JsonPropertyName("image")]
        public string ImageUrl { get; set; }

        [JsonPropertyName("maplink")]
        public string MapLink { get; set; }

        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }

    public class PoiTranslationDto
    {
        [JsonPropertyName("poi_id")]
        public string PoiId { get; set; }

        [JsonPropertyName("lang_code")]
        public string LangCode { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    public class PoiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://poiadmin.rf.gd/api/index.php/";

        public PoiService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15) // Khắc phục tình trạng treo chờ API quá lâu
            };
        }

        public async Task<List<PoiDto>> GetPoisAsync()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{SupabaseSecrets.Url}/rest/v1/pois?select=*");
                request.Headers.Add("apikey", SupabaseSecrets.ApiKey);
                request.Headers.Add("Authorization", $"Bearer {SupabaseSecrets.ApiKey}");
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<List<PoiDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result != null)
                    {
                        return result;
                    }
                    System.Diagnostics.Debug.WriteLine("Supabase returned empty or invalid JSON payload.");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Supabase response {(int)response.StatusCode} {response.ReasonPhrase}: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching POIs: {ex.Message}");
            }

            return new List<PoiDto>();
        }

        public async Task<List<PoiTranslationDto>> GetPoiTranslationsAsync()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{SupabaseSecrets.Url}/rest/v1/poitranslations?select=poi_id,lang_code,description");
                request.Headers.Add("apikey", SupabaseSecrets.ApiKey);
                request.Headers.Add("Authorization", $"Bearer {SupabaseSecrets.ApiKey}");
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<List<PoiTranslationDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result != null)
                    {
                        return result;
                    }
                    System.Diagnostics.Debug.WriteLine("Supabase returned empty or invalid JSON payload for poitranslations.");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Supabase poitranslations response {(int)response.StatusCode} {response.ReasonPhrase}: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching POI translations: {ex.Message}");
            }

            return new List<PoiTranslationDto>();
        }
    }
}