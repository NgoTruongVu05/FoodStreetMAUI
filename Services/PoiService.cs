using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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

        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }

    public class PoiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://poiadmin.rf.gd/api/index.php/";

        public PoiService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5) // Khắc phục tình trạng treo chờ API quá lâu
            };
        }

        public async Task<List<PoiDto>> GetPoisAsync()
        {
            try
            {
                _httpClient.Timeout = TimeSpan.FromSeconds(15);
                var supabaseUrl = "";
                var supabaseApiKey = "";
                var request = new HttpRequestMessage(HttpMethod.Get, $"{supabaseUrl}/rest/v1/pois?select=*");
                request.Headers.Add("apikey", supabaseApiKey);
                request.Headers.Add("Authorization", $"Bearer {supabaseApiKey}");
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
    }
}