using System;
using System.Collections.Generic;

namespace FoodStreetMAUI.Models
{
    public class GpsCoordinate
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Accuracy { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public GpsCoordinate() { }
        public GpsCoordinate(double lat, double lng, double accuracy = 5.0)
        {
            Latitude = lat; Longitude = lng; Accuracy = accuracy;
        }

        public double DistanceTo(GpsCoordinate other)
        {
            const double R = 6371000;
            double dLat = ToRad(other.Latitude - Latitude);
            double dLon = ToRad(other.Longitude - Longitude);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(ToRad(Latitude)) * Math.Cos(ToRad(other.Latitude))
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRad(double deg) => deg * Math.PI / 180;
        public override string ToString() => $"{Latitude:F5}, {Longitude:F5}";
    }

    public enum PoiStatus { Inactive, Approaching, Active }
    public enum TriggerMode { OnEnter, OnApproach, Both }
    public enum ContentType { TextToSpeech, AudioFile }

    public class LocalizedContent
    {
        public string Language { get; set; } = "vi";
        public string LanguageName { get; set; } = "Tiếng Việt";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public ContentType ContentType { get; set; } = ContentType.TextToSpeech;
        public string? AudioFilePath { get; set; }
    }

    public class PointOfInterest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "";
        public string Category { get; set; } = "Ẩm thực";
        public string Emoji { get; set; } = "🍜";
        public GpsCoordinate Location { get; set; } = new(0, 0);
        public double TriggerRadius { get; set; } = 30;
        public double ApproachRadius { get; set; } = 80;
        public int Priority { get; set; } = 5;
        public TriggerMode TriggerMode { get; set; } = TriggerMode.Both;
        public bool IsEnabled { get; set; } = true;
        public PoiStatus Status { get; set; } = PoiStatus.Inactive;
        public Dictionary<string, LocalizedContent> Contents { get; set; } = new();
        public DateTime LastTriggered { get; set; } = DateTime.MinValue;
        public int DebounceSeconds { get; set; } = 120;
        public int MaxDailyTriggers { get; set; } = 3;
        public int TodayTriggerCount { get; set; } = 0;
        public DateTime TriggerCountDate { get; set; } = DateTime.Today;

        public bool CanTrigger()
        {
            if (!IsEnabled) return false;
            if ((DateTime.Now - LastTriggered).TotalSeconds < DebounceSeconds) return false;
            if (TriggerCountDate != DateTime.Today) { TodayTriggerCount = 0; TriggerCountDate = DateTime.Today; }
            return TodayTriggerCount < MaxDailyTriggers;
        }

        public void RecordTrigger()
        {
            LastTriggered = DateTime.Now;
            if (TriggerCountDate != DateTime.Today) { TodayTriggerCount = 0; TriggerCountDate = DateTime.Today; }
            TodayTriggerCount++;
        }

        public LocalizedContent? GetContent(string lang)
        {
            if (Contents.TryGetValue(lang, out var c)) return c;
            if (Contents.TryGetValue("vi", out var fallback)) return fallback;
            return null;
        }

        public string DisplayName => $"{Emoji} {Name}";
    }

    public class TriggerEvent
    {
        public DateTime Time { get; set; } = DateTime.Now;
        public string PoiName { get; set; } = "";
        public string EventType { get; set; } = "Enter";
        public string Language { get; set; } = "vi";
        public double Distance { get; set; }
    }
}
