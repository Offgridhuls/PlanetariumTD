using UnityEngine;
using System;

namespace PlanetariumTD.Audio
{
    public enum GameState
    {
        MainMenu,
        PreRound,
        RoundActive,
        Victory,
        Defeat
    }

    public enum SoundCategory
    {
        Music,
        SoundEffect,
        UI
    }

    [CreateAssetMenu(fileName = "AudioData", menuName = "PlanetariumTD/Audio Data")]
    public class AudioData : ScriptableObject
    {
        [Header("Music")]
        public AudioClip mainMenuMusic;
        public AudioClip gameplayMusic;
        public AudioClip victoryMusic;
        public AudioClip defeatMusic;

        [Header("UI Sounds")]
        public AudioClip buttonClick;
        public AudioClip buttonHover;
        public AudioClip errorSound;
        public AudioClip purchaseSound;

        [Header("Game Sounds")]
        public AudioClip roundStart;
        public AudioClip roundEnd;
        public AudioClip enemyDeath;
        public AudioClip turretFire;
        public AudioClip baseHit;

        [Header("Volume Settings")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.8f;
        [Range(0f, 1f)] public float soundEffectsVolume = 1f;
        [Range(0f, 1f)] public float uiVolume = 0.7f;

        [Header("Audio Settings")]
        public float fadeTime = 1f;
        public int maxSimultaneousSounds = 10;
    }
}
