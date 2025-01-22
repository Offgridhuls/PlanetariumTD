using UnityEngine;
using System;
using System.Collections.Generic;

namespace Planetarium
{
    public class AnnouncerContext
    {
        public int CurrentWave;
        public float CurrentBaseHealth;
        public float MaxBaseHealth;
        public int CurrentScore;
        public int Currency;
        public bool IsWaveInProgress;
        public int EnemiesRemaining;
        public bool IsGameOver;
    }

    public class Announcer : SceneService
    {
        // PUBLIC MEMBERS
        public Action<AnnouncementData> OnAnnouncement;

        // PRIVATE MEMBERS
        [SerializeField]
        private AudioSource audioSource;
        [SerializeField]
        private Announcement[] announcements;

        private AnnouncerContext _context;
        private List<AnnouncementData> _collectedAnnouncements = new List<AnnouncementData>(16);
        private List<AnnouncementData> _waitingAnnouncements = new List<AnnouncementData>(16);
        private GameStateManager _gameState;

        // SceneService INTERFACE
        protected override void OnInitialize()
        {
            base.OnInitialize();
            _gameState = Context.GameState;
            
            if (_gameState != null)
            {
                _gameState.OnGameStateChanged += HandleGameStateChanged;
                _gameState.OnWaveChanged += HandleWaveChanged;
                _gameState.OnBaseHealthChanged += HandleBaseHealthChanged;
                _gameState.OnGameOverChanged += HandleGameOverChanged;
            }
        }

        protected override void OnDeinitialize()
        {
            if (_gameState != null)
            {
                _gameState.OnGameStateChanged -= HandleGameStateChanged;
                _gameState.OnWaveChanged -= HandleWaveChanged;
                _gameState.OnBaseHealthChanged -= HandleBaseHealthChanged;
                _gameState.OnGameOverChanged -= HandleGameOverChanged;
            }
            
            OnAnnouncement = null;
            base.OnDeinitialize();
        }

        protected override void OnTick()
        {
            if (_gameState == null || !IsActive)
                return;

            if (_context == null)
            {
                _context = new AnnouncerContext();
            }

            PrepareContext();

            if (announcements == null || announcements.Length == 0)
                return;

            UpdateAnnouncements();
        }

        protected override void OnDeactivate()
        {
            if (announcements != null)
            {
                for (int i = 0; i < announcements.Length; i++)
                {
                    if (announcements[i] != null)
                    {
                        announcements[i].Deactivate();
                    }
                }
            }

            _context = null;
        }

        // EVENT HANDLERS
        private void HandleGameStateChanged(object sender, GameStateChangedEventArgs e)
        {
            PrepareContext();
        }

        private void HandleWaveChanged(int wave)
        {
            // Add immediate wave announcement if needed
        }

        private void HandleBaseHealthChanged(float health)
        {
            // Add immediate health warning if needed
        }

        private void HandleGameOverChanged(bool isGameOver)
        {
            if (isGameOver)
            {
                var gameOverAnnouncement = new AnnouncementData
                {
                    Text = _gameState.CurrentBaseHealth <= 0 ? "Game Over - Base Destroyed!" : "Victory - All Waves Cleared!",
                    Channel = AnnouncementChannel.GameState,
                    Priority = 10,
                    ValidTime = 5f
                };
                
                _collectedAnnouncements.Add(gameOverAnnouncement);
            }
        }

        // PRIVATE METHODS
        private void PrepareContext()
        {
            _context.CurrentWave = _gameState.CurrentWave;
            _context.CurrentBaseHealth = _gameState.CurrentBaseHealth;
            _context.MaxBaseHealth = _gameState.MaxBaseHealth;
            _context.CurrentScore = _gameState.CurrentScore;
            _context.Currency = _gameState.Currency;
            _context.IsWaveInProgress = _gameState.IsWaveInProgress;
            _context.EnemiesRemaining = _gameState.EnemiesRemainingInWave;
            _context.IsGameOver = _gameState.IsGameOver;
        }

        private void UpdateAnnouncements()
        {
            float deltaTime = Time.deltaTime;

            // Collect new announcements
            for (int i = 0; i < announcements.Length; i++)
            {
                Announcement announcement = announcements[i];
                if (announcement == null || announcement.IsFinished)
                    continue;

                announcement.Tick(_context, _collectedAnnouncements);
            }

            // Add new announcements to waiting queue
            if (_collectedAnnouncements.Count > 0)
            {
                AddAnnouncements(_collectedAnnouncements);
                _collectedAnnouncements.Clear();
            }

            // Update cooldowns
            for (int i = _waitingAnnouncements.Count - 1; i >= 0; i--)
            {
                var announcement = _waitingAnnouncements[i];

                if (announcement.Cooldown > 0f)
                {
                    announcement.Cooldown -= deltaTime;
                    _waitingAnnouncements[i] = announcement;
                }
            }

            // Try to announce
            if (_waitingAnnouncements.Count > 0 && TryAnnounce(_waitingAnnouncements[0]))
            {
                _waitingAnnouncements.RemoveAt(0);
            }

            // Update validity timers and remove expired announcements
            for (int i = _waitingAnnouncements.Count - 1; i >= 0; i--)
            {
                var announcement = _waitingAnnouncements[i];

                if (announcement.ValidCooldown > 0f)
                {
                    announcement.ValidCooldown -= deltaTime;
                    _waitingAnnouncements[i] = announcement;
                }
                else
                {
                    _waitingAnnouncements.RemoveAt(i);
                }
            }
        }

        private void AddAnnouncements(List<AnnouncementData> newAnnouncements)
        {
            if (newAnnouncements.Count == 0)
                return;

            for (int i = 0; i < newAnnouncements.Count; i++)
            {
                var newAnnouncement = newAnnouncements[i];
                newAnnouncement.ValidCooldown = newAnnouncement.ValidTime;

                bool add = true;

                // Replace existing announcements of same channel if priority is higher or equal
                for (int j = 0; j < _waitingAnnouncements.Count; j++)
                {
                    if (_waitingAnnouncements[j].Channel == newAnnouncement.Channel)
                    {
                        if (_waitingAnnouncements[j].Priority <= newAnnouncement.Priority)
                        {
                            _waitingAnnouncements[j] = newAnnouncement;
                        }
                        add = false;
                    }
                }

                if (add)
                {
                    _waitingAnnouncements.Add(newAnnouncement);
                }
            }

            // Sort by priority (highest first)
            _waitingAnnouncements.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        private bool TryAnnounce(AnnouncementData announcement)
        {
            if (announcement.Cooldown > 0f)
                return false;

            if (announcement.AudioClips != null && announcement.AudioClips.Length > 0 && audioSource != null)
            {
                if (audioSource.isPlaying)
                    return false;

                audioSource.clip = announcement.AudioClips[UnityEngine.Random.Range(0, announcement.AudioClips.Length)];
                audioSource.Play();
            }

            OnAnnouncement?.Invoke(announcement);
            return true;
        }
    }
}
