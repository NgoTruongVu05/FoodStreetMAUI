using System;
using System.Collections.Generic;
using System.Linq;
using FoodStreetMAUI.Models;

namespace FoodStreetMAUI.Services
{
    public class GeofenceEventArgs : EventArgs
    {
        public PointOfInterest Poi { get; }
        public string EventType { get; }
        public double Distance { get; }
        public GpsCoordinate UserLocation { get; }
        public GeofenceEventArgs(PointOfInterest poi, string evt, double dist, GpsCoordinate loc)
        { Poi = poi; EventType = evt; Distance = dist; UserLocation = loc; }
    }

    public class GeofenceService
    {
        public event EventHandler<GeofenceEventArgs>? GeofenceTriggered;
        public event EventHandler<string>? LogMessage;

        private readonly List<PointOfInterest> _pois = new();
        private readonly Dictionary<Guid, PoiStatus> _prevStatus = new();
        private readonly Dictionary<Guid, DateTime> _lastEnter = new();
        private readonly Dictionary<Guid, DateTime> _lastApproach = new();

        public IReadOnlyList<PointOfInterest> Pois => _pois.AsReadOnly();

        public void AddPoi(PointOfInterest poi)
        {
            _pois.Add(poi);
            _prevStatus[poi.Id] = PoiStatus.Inactive;
        }

        public void ClearAll()
        {
            _pois.Clear();
            _prevStatus.Clear();
            _lastEnter.Clear();
            _lastApproach.Clear();
        }

        public void UpdateLocation(GpsCoordinate location)
        {
            foreach (var poi in _pois.Where(p => p.IsEnabled)
                                     .OrderByDescending(p => p.Priority))
            {
                double dist = location.DistanceTo(poi.Location);
                var newStatus = dist <= poi.TriggerRadius ? PoiStatus.Active
                              : dist <= poi.ApproachRadius ? PoiStatus.Approaching
                              : PoiStatus.Inactive;

                poi.Status = newStatus;
                var prev = _prevStatus.TryGetValue(poi.Id, out var ps) ? ps : PoiStatus.Inactive;

                if (newStatus == PoiStatus.Active && prev != PoiStatus.Active)
                {
                    if (ShouldFireEnter(poi))
                    {
                        poi.RecordTrigger();
                        _lastEnter[poi.Id] = DateTime.Now;
                        LogMessage?.Invoke(this, $"✅ ENTER: {poi.Emoji} {poi.Name} ({dist:F0}m)");
                        GeofenceTriggered?.Invoke(this, new GeofenceEventArgs(poi, "Enter", dist, location));
                    }
                }
                else if (newStatus == PoiStatus.Approaching && prev == PoiStatus.Inactive
                         && poi.TriggerMode != TriggerMode.OnEnter)
                {
                    if (ShouldFireApproach(poi))
                    {
                        _lastApproach[poi.Id] = DateTime.Now;
                        LogMessage?.Invoke(this, $"📍 APPROACH: {poi.Emoji} {poi.Name} ({dist:F0}m)");
                        GeofenceTriggered?.Invoke(this, new GeofenceEventArgs(poi, "Approach", dist, location));
                    }
                }
                else if (newStatus == PoiStatus.Inactive && prev != PoiStatus.Inactive)
                {
                    LogMessage?.Invoke(this, $"↩️ EXIT: {poi.Name}");
                }

                _prevStatus[poi.Id] = newStatus;
            }
        }

        private bool ShouldFireEnter(PointOfInterest poi)
        {
            if (!poi.CanTrigger()) return false;
            if (_lastEnter.TryGetValue(poi.Id, out var last)
                && (DateTime.Now - last).TotalSeconds < poi.DebounceSeconds) return false;
            return poi.TriggerMode != TriggerMode.OnApproach;
        }

        private bool ShouldFireApproach(PointOfInterest poi)
        {
            if (_lastApproach.TryGetValue(poi.Id, out var last)
                && (DateTime.Now - last).TotalSeconds < poi.DebounceSeconds) return false;
            return true;
        }

        public List<(PointOfInterest poi, double dist)> GetNearby(GpsCoordinate loc, double maxDist = 1000)
            => _pois
               .Select(p => (p, loc.DistanceTo(p.Location)))
               .Where(x => x.Item2 <= maxDist)
               .OrderBy(x => x.Item2)
               .ToList();
    }
}
