using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FoodStreetMAUI.Models;
using Newtonsoft.Json;
using SQLite;

namespace FoodStreetMAUI.Services
{
    public class DataService
    {
        private static string PoiDbPath =>
            Path.Combine(FileSystem.AppDataDirectory, "pois.db3");

        private SQLiteAsyncConnection _db;

        private async Task InitDb()
        {
            if (_db != null) return;
            _db = new SQLiteAsyncConnection(PoiDbPath);
            await _db.CreateTableAsync<PoiEntity>();

            var columns = await _db.QueryAsync<TableInfo>("PRAGMA table_info(PoiEntity);");
            if (!columns.Any(c => string.Equals(c.Name, nameof(PoiEntity.ImageUrl), StringComparison.OrdinalIgnoreCase)))
            {
                await _db.ExecuteAsync($"ALTER TABLE {nameof(PoiEntity)} ADD COLUMN {nameof(PoiEntity.ImageUrl)} TEXT");
            }
        }

        public async Task<List<PointOfInterest>> LoadPoisAsync()
        {
            try
            {
                await InitDb();

                // Always sync with API when loading
                await SyncPoisFromApiAsync();

                var entities = await _db.Table<PoiEntity>().ToListAsync();

                if (entities.Count > 0)
                {
                    var pois = new List<PointOfInterest>();
                    foreach (var entity in entities)
                    {
                        var poi = new PointOfInterest
                        {
                            Id = Guid.TryParse(entity.Id, out var guid) ? guid : Guid.NewGuid(),
                            Name = entity.Name ?? "",
                            Category = entity.Category ?? "",
                            ImageUrl = entity.ImageUrl ?? "",
                            Location = new GpsCoordinate(entity.Lat, entity.Lng),
                            TriggerRadius = 25,
                            ApproachRadius = 70,
                            Priority = 8,
                            DebounceSeconds = 150
                        };

                        poi.Contents["vi"] = new LocalizedContent 
                        { 
                            Language = "vi", 
                            LanguageName = "Tiếng Việt",
                            Title = entity.Name ?? "",
                            Description = entity.Description ?? ""
                        };

                        pois.Add(poi);
                    }
                    return pois;
                }
            }
            catch { }
            return new List<PointOfInterest>();
        }

        public async Task SavePoisAsync(List<PointOfInterest> pois)
        {
            try
            {
                await InitDb();
                await _db.DeleteAllAsync<PoiEntity>();
                var entities = new List<PoiEntity>();
                foreach (var p in pois)
                {
                    var desc = p.GetContent("vi")?.Description ?? "";
                    entities.Add(new PoiEntity 
                    { 
                        Id = p.Id.ToString(), 
                        Name = p.Name,
                        Description = desc,
                        Category = p.Category,
                        ImageUrl = p.ImageUrl,
                        Lat = p.Location.Latitude,
                        Lng = p.Location.Longitude
                    });
                }
                await _db.InsertAllAsync(entities);
            }
            catch { }
        }

        public async Task SyncPoisFromApiAsync()
        {
            try
            {
                var poiService = new PoiService();
                var apiPois = await poiService.GetPoisAsync();

                if (apiPois != null && apiPois.Count > 0)
                {
                    await InitDb();
                    await _db.DeleteAllAsync<PoiEntity>();

                    var entities = new List<PoiEntity>();
                    foreach (var dto in apiPois)
                    {
                        entities.Add(new PoiEntity 
                        { 
                            Id = dto.Id ?? Guid.NewGuid().ToString(), 
                            Name = dto.Name,
                            Description = dto.Description,
                            Category = string.Empty,
                            ImageUrl = dto.ImageUrl,
                            Lat = dto.Lat,
                            Lng = dto.Lng
                        });
                    }
                    await _db.InsertAllAsync(entities);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error syncing from API: {ex.Message}");
            }
        }

        public class PoiEntity
        {
            [SQLite.PrimaryKey]
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public string ImageUrl { get; set; }
            public double Lat { get; set; }
            public double Lng { get; set; }
        }

        private sealed class TableInfo
        {
            [SQLite.Column("name")]
            public string Name { get; set; }
        }
    }
}
