using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace PlanetariumTD.Audio
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private AudioData audioData;
        [SerializeField] private AudioMixer audioMixer;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource uiSource;
        
        private Queue<AudioSource> soundPool;
        private GameState currentState;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudio();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Default to PreRound state, override in menu scene if needed
            SetGameState(GameState.PreRound);
        }

        private void InitializeAudio()
        {
            // Initialize sound pool
            soundPool = new Queue<AudioSource>();
            for (int i = 0; i < audioData.maxSimultaneousSounds; i++)
            {
                CreatePooledAudioSource();
            }

            // Set up music source
            musicSource.loop = true;

            // Initialize volumes
            SetVolume(SoundCategory.Music, audioData.musicVolume);
            SetVolume(SoundCategory.SoundEffect, audioData.soundEffectsVolume);
            SetVolume(SoundCategory.UI, audioData.uiVolume);
        }

        private void CreatePooledAudioSource()
        {
            GameObject obj = new GameObject($"SoundEffect_{soundPool.Count}");
            obj.transform.SetParent(transform);
            AudioSource source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            soundPool.Enqueue(source);
        }

        public void SetGameState(GameState newState)
        {
            if (currentState == newState) return;
            currentState = newState;

            // Handle music changes
            AudioClip newMusic = newState switch
            {
                GameState.MainMenu => audioData.mainMenuMusic,
                GameState.Victory => audioData.victoryMusic,
                GameState.Defeat => audioData.defeatMusic,
                _ => audioData.gameplayMusic
            };

            StartCoroutine(FadeMusic(newMusic));
        }

        private System.Collections.IEnumerator FadeMusic(AudioClip newMusic)
        {
            if (musicSource.clip != newMusic)
            {
                // Fade out
                float startVolume = musicSource.volume;
                float timer = 0;
                
                while (timer < audioData.fadeTime)
                {
                    timer += Time.deltaTime;
                    musicSource.volume = Mathf.Lerp(startVolume, 0, timer / audioData.fadeTime);
                    yield return null;
                }

                // Change clip
                musicSource.clip = newMusic;
                musicSource.Play();

                // Fade in
                timer = 0;
                while (timer < audioData.fadeTime)
                {
                    timer += Time.deltaTime;
                    musicSource.volume = Mathf.Lerp(0, audioData.musicVolume, timer / audioData.fadeTime);
                    yield return null;
                }
            }
        }

        public void PlaySound(AudioClip clip, SoundCategory category, Vector3? position = null)
        {
            if (clip == null) return;

            switch (category)
            {
                case SoundCategory.UI:
                    uiSource.PlayOneShot(clip);
                    break;
                case SoundCategory.SoundEffect:
                    PlayPooledSound(clip, position);
                    break;
            }
        }

        private void PlayPooledSound(AudioClip clip, Vector3? position)
        {
            if (soundPool.Count == 0) return;

            AudioSource source = soundPool.Dequeue();
            source.clip = clip;
            source.volume = audioData.soundEffectsVolume;

            if (position.HasValue)
            {
                source.transform.position = position.Value;
                source.spatialBlend = 1f;
            }
            else
            {
                source.spatialBlend = 0f;
            }

            source.Play();
            StartCoroutine(ReturnToPool(source, clip.length));
        }

        private System.Collections.IEnumerator ReturnToPool(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);
            soundPool.Enqueue(source);
        }

        public void SetVolume(SoundCategory category, float volume)
        {
            string parameter = category switch
            {
                SoundCategory.Music => "MusicVolume",
                SoundCategory.SoundEffect => "SFXVolume",
                SoundCategory.UI => "UIVolume",
                _ => "MasterVolume"
            };

            float decibelValue = volume > 0 ? 20f * Mathf.Log10(volume) : -80f;
            audioMixer.SetFloat(parameter, decibelValue);
        }

        #region Utility Methods
        public void PlayButtonClick() => PlaySound(audioData.buttonClick, SoundCategory.UI);
        public void PlayButtonHover() => PlaySound(audioData.buttonHover, SoundCategory.UI);
        public void PlayError() => PlaySound(audioData.errorSound, SoundCategory.UI);
        public void PlayPurchase() => PlaySound(audioData.purchaseSound, SoundCategory.UI);
        public void PlayRoundStart() => PlaySound(audioData.roundStart, SoundCategory.SoundEffect);
        public void PlayRoundEnd() => PlaySound(audioData.roundEnd, SoundCategory.SoundEffect);
        public void PlayEnemyDeath(Vector3 position) => PlaySound(audioData.enemyDeath, SoundCategory.SoundEffect, position);
        public void PlayTurretFire(Vector3 position) => PlaySound(audioData.turretFire, SoundCategory.SoundEffect, position);
        public void PlayBaseHit() => PlaySound(audioData.baseHit, SoundCategory.SoundEffect);
        #endregion
    }
}
