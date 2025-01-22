using UnityEngine;
using System;

namespace Planetarium
{
    public enum AnnouncementChannel
    {
        Wave,
        Base,
        Achievement,
        GameState
    }

    [Serializable]
    public struct AnnouncementData
    {
        public string Text;
        public AudioClip[] AudioClips;
        public AnnouncementChannel Channel;
        public int Priority;
        public float ValidTime;
        
        [NonSerialized]
        public float ValidCooldown;
        [NonSerialized]
        public float Cooldown;
    }

    [CreateAssetMenu(fileName = "Announcement", menuName = "PlanetariumTD/Announcement")]
    public abstract class Announcement : ScriptableObject
    {
        public bool IsFinished { get; protected set; }

        public virtual void Activate(AnnouncerContext context) { }
        public virtual void Deactivate() { }
        public virtual void Tick(AnnouncerContext context, System.Collections.Generic.List<AnnouncementData> announcements) { }
    }

    // Example announcement types
    [CreateAssetMenu(fileName = "WaveAnnouncement", menuName = "PlanetariumTD/WaveAnnouncement")]
    public class WaveAnnouncement : Announcement
    {
        [SerializeField] private AudioClip[] waveStartClips;
        [SerializeField] private AudioClip[] waveClearedClips;
        
        private int lastWave = -1;

        public override void Tick(AnnouncerContext context, System.Collections.Generic.List<AnnouncementData> announcements)
        {
            if (context.CurrentWave != lastWave)
            {
                if (lastWave >= 0)
                {
                    // Wave cleared announcement
                    announcements.Add(new AnnouncementData
                    {
                        Text = $"Wave {lastWave} Cleared!",
                        AudioClips = waveClearedClips,
                        Channel = AnnouncementChannel.Wave,
                        Priority = 1,
                        ValidTime = 3f
                    });
                }

                // New wave announcement
                announcements.Add(new AnnouncementData
                {
                    Text = $"Wave {context.CurrentWave} Incoming!",
                    AudioClips = waveStartClips,
                    Channel = AnnouncementChannel.Wave,
                    Priority = 2,
                    ValidTime = 3f
                });

                lastWave = context.CurrentWave;
            }
        }
    }

    [CreateAssetMenu(fileName = "BaseAnnouncement", menuName = "PlanetariumTD/BaseAnnouncement")]
    public class BaseAnnouncement : Announcement
    {
        [SerializeField] private AudioClip[] baseDamageClips;
        [SerializeField] private float healthThreshold = 0.25f;
        
        private bool lowHealthWarningIssued;

        public override void Tick(AnnouncerContext context, System.Collections.Generic.List<AnnouncementData> announcements)
        {
            float healthPercentage = context.CurrentBaseHealth / context.MaxBaseHealth;
            
            if (healthPercentage <= healthThreshold && !lowHealthWarningIssued)
            {
                announcements.Add(new AnnouncementData
                {
                    Text = "Warning: Base Health Critical!",
                    AudioClips = baseDamageClips,
                    Channel = AnnouncementChannel.Base,
                    Priority = 3,
                    ValidTime = 5f
                });
                
                lowHealthWarningIssued = true;
            }
            else if (healthPercentage > healthThreshold)
            {
                lowHealthWarningIssued = false;
            }
        }
    }
}
