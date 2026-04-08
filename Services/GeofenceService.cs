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
        private readonly object _sync = new();
        public event EventHandler<GeofenceEventArgs>? GeofenceTriggered;
        public event EventHandler<string>? LogMessage;

        private readonly List<PointOfInterest> _pois = new();
        private readonly Dictionary<Guid, PoiStatus> _prevPrevStatus = new();
        private readonly Dictionary<Guid, PoiStatus> _prevStatus = new();
        private readonly Dictionary<Guid, DateTime> _lastEnter = new();
        private readonly Dictionary<Guid, DateTime> _lastApproach = new();

        public IReadOnlyList<PointOfInterest> Pois
        {
            get
            {
                lock (_sync)
                    return _pois.ToList().AsReadOnly();
            }
        }

        public void AddPoi(PointOfInterest poi)
        {
            lock (_sync)
            {
                _pois.Add(poi);
                _prevStatus[poi.Id] = PoiStatus.Inactive;
            }
        }

        public void ClearAll()
        {
            lock (_sync)
            {
                _pois.Clear();
                _prevPrevStatus.Clear();
                _prevStatus.Clear();
                _lastEnter.Clear();
                _lastApproach.Clear();
            }
        }

        public void UpdateLocation(GpsCoordinate location)
        {
            var logs = new List<string>();
            var events = new List<GeofenceEventArgs>();

            lock (_sync)
            {
                var infos = _pois.Where(p => p.IsEnabled)
                                 .Select(p =>
                                 {
                                     var dist = location.DistanceTo(p.Location);
                                     var newStatus = dist <= p.TriggerRadius ? PoiStatus.Active
                                                   : dist <= p.ApproachRadius ? PoiStatus.Approaching
                                                   : PoiStatus.Inactive;
                                     var prev = _prevStatus.TryGetValue(p.Id, out var ps) ? ps : PoiStatus.Inactive;
                                     return (poi: p, dist, newStatus, prev);
                                 })
                                 .ToList();

                // Determine triggered POIs (Active or Approaching), choose nearest among them
                var triggered = infos.Where(i => i.newStatus == PoiStatus.Active || i.newStatus == PoiStatus.Approaching)
                                     .OrderBy(i => i.newStatus == PoiStatus.Active ? 0 : 1)
                                     .ThenBy(i => i.dist)
                                     .ToList();

                var nearest = triggered.FirstOrDefault();

                // Fire event only for the nearest triggered POI (if any) to ensure only its audio plays
                if (nearest.poi != null)
                {
                    var i = nearest;
                    if (i.newStatus == PoiStatus.Active && i.prev != PoiStatus.Active)
                    {
                        if (ShouldFireEnter(i.poi))
                        {
                            i.poi.RecordTrigger();
                            _lastEnter[i.poi.Id] = DateTime.Now;
                            logs.Add($"✅ ENTER: {i.poi.Emoji} {i.poi.Name} ({i.dist:F0}m)");
                            events.Add(new GeofenceEventArgs(i.poi, "Enter", i.dist, location));
                        }
                    }
                    else if (i.newStatus == PoiStatus.Approaching && i.prev == PoiStatus.Inactive
                             && i.poi.TriggerMode != TriggerMode.OnEnter)
                    {
                        if (ShouldFireApproach(i.poi))
                        {
                            _lastApproach[i.poi.Id] = DateTime.Now;
                            logs.Add($"📍 APPROACH: {i.poi.Emoji} {i.poi.Name} ({i.dist:F0}m)");
                            events.Add(new GeofenceEventArgs(i.poi, "Approach", i.dist, location));
                        }
                    }
                }

                // Update statuses and prev status for all POIs; log exits
                foreach (var info in infos)
                {
                    if (info.newStatus == PoiStatus.Inactive && info.prev != PoiStatus.Inactive)
                        logs.Add($"↩️ EXIT: {info.poi.Name}");

                    _prevPrevStatus[info.poi.Id] = info.prev;
                    _prevStatus[info.poi.Id] = info.newStatus;
                    info.poi.Status = info.newStatus;
                }
            }

            foreach (var log in logs)
                LogMessage?.Invoke(this, log);
            foreach (var evt in events)
                GeofenceTriggered?.Invoke(this, evt);
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
        {
            lock (_sync)
            {
                return _pois
                    .Select(p => (p, loc.DistanceTo(p.Location)))
                    .Where(x => x.Item2 <= maxDist)
                    .OrderBy(x => x.Item2)
                    .ToList();
            }
        }
    }
}
