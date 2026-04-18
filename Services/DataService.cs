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
            await _db.CreateTableAsync<PoiTranslationEntity>();

            var columns = await _db.QueryAsync<TableInfo>("PRAGMA table_info(PoiEntity);");
            if (!columns.Any(c => string.Equals(c.Name, nameof(PoiEntity.ImageUrl), StringComparison.OrdinalIgnoreCase)))
            {
                await _db.ExecuteAsync($"ALTER TABLE {nameof(PoiEntity)} ADD COLUMN {nameof(PoiEntity.ImageUrl)} TEXT");
            }

            if (!columns.Any(c => string.Equals(c.Name, nameof(PoiEntity.MapLink), StringComparison.OrdinalIgnoreCase)))
            {
                await _db.ExecuteAsync($"ALTER TABLE {nameof(PoiEntity)} ADD COLUMN {nameof(PoiEntity.MapLink)} TEXT");
            }

            if (!columns.Any(c => string.Equals(c.Name, nameof(PoiEntity.Priority), StringComparison.OrdinalIgnoreCase)))
            {
                await _db.ExecuteAsync($"ALTER TABLE {nameof(PoiEntity)} ADD COLUMN {nameof(PoiEntity.Priority)} INTEGER NOT NULL DEFAULT 0");
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
                var translations = await _db.Table<PoiTranslationEntity>().ToListAsync();
                var translationsByPoi = translations
                    .Where(t => !string.IsNullOrWhiteSpace(t.PoiId))
                    .GroupBy(t => t.PoiId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                if (entities.Count > 0)
                {
                    var pois = new List<PointOfInterest>();
                    foreach (var entity in entities)
                    {
                        var poi = new PointOfInterest
                        {
                            Id = Guid.TryParse(entity.Id, out var guid) ? guid : Guid.NewGuid(),
                            ExternalId = entity.Id,
                            Name = entity.Name ?? "",
                            Category = entity.Category ?? "",
                            ImageUrl = entity.ImageUrl ?? "",
                            MapLink = entity.MapLink,
                            Location = new GpsCoordinate(entity.Lat, entity.Lng),
                            TriggerRadius = 25,
                            ApproachRadius = 70,
                            Priority = entity.Priority,
                            DebounceSeconds = 150
                        };

                        if (translationsByPoi.TryGetValue(entity.Id ?? string.Empty, out var poiTranslations))
                        {
                            foreach (var translation in poiTranslations)
                            {
                                var lang = string.IsNullOrWhiteSpace(translation.LangCode) ? "vi" : translation.LangCode;
                                var languageName = string.Equals(lang, "vi", StringComparison.OrdinalIgnoreCase)
                                    ? "Tiếng Việt"
                                    : lang.ToUpperInvariant();
                                poi.Contents[lang] = new LocalizedContent
                                {
                                    Language = lang,
                                    LanguageName = languageName,
                                    Title = entity.Name ?? "",
                                    Description = translation.Description ?? ""
                                };
                            }
                        }

                        if (!poi.Contents.ContainsKey("vi"))
                        {
                            poi.Contents["vi"] = new LocalizedContent
                            {
                                Language = "vi",
                                LanguageName = "Tiếng Việt",
                                Title = entity.Name ?? "",
                                Description = ""
                            };
                        }

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
                        MapLink = p.MapLink,
                        Lat = p.Location.Latitude,
                        Lng = p.Location.Longitude,
                        Priority = p.Priority
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
                var apiTranslations = await poiService.GetPoiTranslationsAsync();

                if (apiPois != null && apiPois.Count > 0)
                {
                    await InitDb();
                    await _db.DeleteAllAsync<PoiEntity>();
                    await _db.DeleteAllAsync<PoiTranslationEntity>();

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
                            MapLink = dto.MapLink,
                            Lat = dto.Lat,
                            Lng = dto.Lng,
                            Priority = dto.Priority
                        });
                    }
                    await _db.InsertAllAsync(entities);

                    if (apiTranslations != null && apiTranslations.Count > 0)
                    {
                        var translationEntities = new List<PoiTranslationEntity>();
                        foreach (var dto in apiTranslations)
                        {
                            if (string.IsNullOrWhiteSpace(dto?.PoiId) || string.IsNullOrWhiteSpace(dto.LangCode))
                            {
                                continue;
                            }

                            translationEntities.Add(new PoiTranslationEntity
                            {
                                PoiId = dto.PoiId,
                                LangCode = dto.LangCode,
                                Description = dto.Description
                            });
                        }

                        if (translationEntities.Count > 0)
                        {
                            await _db.InsertAllAsync(translationEntities);
                        }
                    }
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
            public string MapLink { get; set; }
            public double Lat { get; set; }
            public double Lng { get; set; }
            public int Priority { get; set; }
        }

        [SQLite.Table("poitranslations")]
        public class PoiTranslationEntity
        {
            [SQLite.Column("poi_id")]
            public string PoiId { get; set; }

            [SQLite.Column("lang_code")]
            public string LangCode { get; set; }

            [SQLite.Column("description")]
            public string Description { get; set; }
        }

        private sealed class TableInfo
        {
            [SQLite.Column("name")]
            public string Name { get; set; }
        }
    }
}
