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

        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }

    public class PoiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://10.0.2.2/POI_Admin/api/index.php/";

        public PoiService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<List<PoiDto>> GetPoisAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}pois");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ApiResponse<List<PoiDto>>>(content);
                    
                    if (result != null && result.Ok)
                    {
                        return result.Data;
                    }
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