using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FoodMapApp.Models;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;
using System.Diagnostics;

namespace FoodMapApp.Services
{
    public class AutoAudioService
    {
        private static AutoAudioService? _instance;
        public static AutoAudioService Instance => _instance ??= new AutoAudioService();

        private List<AudioQueueItem> _queue = new();
        public IReadOnlyList<AudioQueueItem> Queue => _queue;
        private Dictionary<int, DateTime> _cooldowns = new();
        private const int MaxQueueSize = 3;
        private const int CooldownMinutes = 15;
        private Location? _lastLocation;

        public event Action<AudioQueueItem?, List<AudioQueueItem>>? OnStateChanged;

        public AudioQueueItem? CurrentItem { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsCallActive { get; private set; }

        private AutoAudioService() { }

        public class AudioQueueItem
        {
            public FoodModel Poi { get; set; } = new();
            public double Score { get; set; }
            public bool IsHeard { get; set; }
            public int CurrentSentenceIndex { get; set; }
            public int TotalSentences { get; set; }
            public string Language { get; set; } = "vi";
        }

        public void UpdateQueue(Location userLocation, List<FoodModel> allPois, string lang)
        {
            if (IsCallActive) return;

            // GPS Optimization Check (handled by caller, but we store location here)
            _lastLocation = userLocation;

            // 1. Filter POIs in range
            var inRange = allPois.Where(p => 
            {
                double dist = Location.CalculateDistance(userLocation, p.latitude, p.longitude, DistanceUnits.Kilometers) * 1000;
                return dist <= (p.range_meters > 0 ? p.range_meters : 50);
            }).ToList();

            // 2. Handle Current Item Exit
            if (CurrentItem != null && !inRange.Any(p => p.id == CurrentItem.Poi.id))
            {
                double progress = CurrentItem.TotalSentences > 0 
                    ? (double)CurrentItem.CurrentSentenceIndex / CurrentItem.TotalSentences 
                    : 0;
                if (progress < 0.5)
                {
                    // User requirement: Left quickly and heard < 50% -> fade out then skip
                    if (MainPage.Instance != null) MainPage.Instance.StopAudioWithFade();
                    CurrentItem = null;
                }
                else
                {
                    // > 50% -> let it finish
                }
            }

            // 3. Remove items from queue that are no longer in range (unless currently playing)
            _queue.RemoveAll(q => q != CurrentItem && !inRange.Any(p => p.id == q.Poi.id));

            // 3. Add new items to queue if not already there and not in cooldown
            foreach (var poi in inRange)
            {
                if (_queue.Any(q => q.Poi.id == poi.id)) continue;
                if (CurrentItem?.Poi.id == poi.id) continue;

                // Check Cooldown
                if (_cooldowns.TryGetValue(poi.id, out var lastHeard))
                {
                    if ((DateTime.Now - lastHeard).TotalMinutes < CooldownMinutes)
                        continue;
                }

                _queue.Add(new AudioQueueItem { Poi = poi, Language = lang });
            }

            // 4. Calculate scores and Sort
            foreach (var item in _queue)
            {
                if (item == CurrentItem) continue;
                item.Score = CalculateScore(item.Poi, userLocation);
            }

            // Keep current item at front, sort rest by score
            var sortedQueue = _queue.Where(q => q == CurrentItem).ToList();
            var others = _queue.Where(q => q != CurrentItem)
                               .OrderByDescending(q => q.Score)
                               .Take(MaxQueueSize - (CurrentItem != null ? 1 : 0))
                               .ToList();
            
            _queue = sortedQueue.Concat(others).ToList();

            OnStateChanged?.Invoke(CurrentItem, _queue);

            // 5. Auto-start if nothing playing
            if (CurrentItem == null && _queue.Count > 0 && !IsPaused)
            {
                _ = PlayNextAsync();
            }
        }

        private double CalculateScore(FoodModel poi, Location userLoc)
        {
            double dist = Location.CalculateDistance(userLoc, poi.latitude, poi.longitude, DistanceUnits.Kilometers) * 1000;
            double range = poi.range_meters > 0 ? poi.range_meters : 50;
            double distScore = Math.Max(0, 1.0 - (dist / range));

            int listens = Preferences.Default.Get($"listen_count_{poi.id}", 0);
            double listenScore = Math.Min(listens, 20) / 20.0; // Assume 20 listens is max popularity

            return distScore * 0.5 + listenScore * 0.5;
        }

        private bool _isPlayingNext = false;

        public async Task PlayNextAsync()
        {
            if (_isPlayingNext) return;
            
            if (_queue.Count == 0 || IsCallActive) 
            {
                CurrentItem = null;
                OnStateChanged?.Invoke(null, _queue);
                return;
            }

            _isPlayingNext = true;
            try
            {
                // User requirement: Wait 3s before starting a new auto-audio
                await Task.Delay(3000);

                if (_queue.Count == 0 || IsCallActive) 
                {
                    CurrentItem = null;
                    return;
                }

                CurrentItem = _queue[0];
                IsPaused = false;
                OnStateChanged?.Invoke(CurrentItem, _queue);

                // Fetch Guide from API and trigger playback via MainPage
                if (MainPage.Instance != null && CurrentItem != null)
                {
                    await MainPage.Instance.TriggerAutoAudioAsync(CurrentItem);
                }
            }
            finally
            {
                _isPlayingNext = false;
            }
        }

        public void MarkAsHeard(int poiId)
        {
            _cooldowns[poiId] = DateTime.Now;
            int count = Preferences.Default.Get($"listen_count_{poiId}", 0);
            Preferences.Default.Set($"listen_count_{poiId}", count + 1);
            
            if (CurrentItem?.Poi.id == poiId)
            {
                _queue.Remove(CurrentItem);
                CurrentItem = null;
                _ = PlayNextAsync();
            }
        }

        /// <summary>
        /// Case 6: Đánh dấu đã nghe (cooldown) nhưng KHÔNG tự phát quán tiếp theo.
        /// Dùng khi user bấm X trên auto audio.
        /// </summary>
        public void MarkAsHeardOnly(int poiId)
        {
            _cooldowns[poiId] = DateTime.Now;
            int count = Preferences.Default.Get($"listen_count_{poiId}", 0);
            Preferences.Default.Set($"listen_count_{poiId}", count + 1);
            
            if (CurrentItem?.Poi.id == poiId)
            {
                _queue.Remove(CurrentItem);
                CurrentItem = null;
            }
            OnStateChanged?.Invoke(CurrentItem, _queue);
        }

        public void RemoveFromQueue(int poiId)
        {
            _queue.RemoveAll(q => q.Poi.id == poiId);
            if (CurrentItem?.Poi.id == poiId)
            {
                CurrentItem = null;
                _ = PlayNextAsync();
            }
            OnStateChanged?.Invoke(CurrentItem, _queue);
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            if (!paused && CurrentItem == null && _queue.Count > 0)
            {
                _ = PlayNextAsync();
            }
        }

        public void SetCallStatus(bool active)
        {
            IsCallActive = active;
            if (active)
            {
                if (MainPage.Instance != null) MainPage.Instance.HandleSystemInterruption();
            }
            else
            {
                if (MainPage.Instance != null) _ = MainPage.Instance.HandleCallEndAsync();
            }
        }

        public bool IsStillInRange(FoodModel poi)
        {
            if (_lastLocation == null) return true;
            double dist = Location.CalculateDistance(_lastLocation, poi.latitude, poi.longitude, DistanceUnits.Kilometers) * 1000;
            return dist <= (poi.range_meters > 0 ? poi.range_meters : 50);
        }

        public void ResetCooldown(int poiId)
        {
            _cooldowns.Remove(poiId);
        }

        public void ClearQueue()
        {
            _queue.Clear();
            CurrentItem = null;
            OnStateChanged?.Invoke(null, _queue);
        }
    }
}
