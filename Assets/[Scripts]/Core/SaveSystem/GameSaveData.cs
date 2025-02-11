using System;
using System.Collections.Generic;
using UnityEngine;

namespace Planetarium.SaveSystem
{
    [Serializable]
    public class GameSaveData
    {
        public string playerName;
        public int currency;
        public float playtime;
        public string lastSaveTime; 
        public List<SolarSystemData> unlockedSystems;
        public string currentSystem;
        public StringBoolDictionary achievements;
        public PlayerProgressData playerProgress;

        public GameSaveData()
        {
            unlockedSystems = new List<SolarSystemData>();
            achievements = new StringBoolDictionary();
            playerProgress = new PlayerProgressData();
            lastSaveTime = DateTime.Now.ToString("o"); 
        }

        public DateTime GetLastSaveTime()
        {
            return DateTime.Parse(lastSaveTime);
        }
    }

    [Serializable]
    public class SolarSystemData
    {
        public string systemId;
        public string systemName;
        public bool isUnlocked;
        public List<PlanetLevelData> planets;
        public Vector3 position; 
        public float difficulty; 
        public List<string> availableTurrets; 
        public List<string> availableResources; 

        public SolarSystemData()
        {
            planets = new List<PlanetLevelData>();
            availableTurrets = new List<string>();
            availableResources = new List<string>();
        }
    }

    [Serializable]
    public class PlanetLevelData
    {
        public string planetId;
        public string planetName;
        public bool isUnlocked;
        public bool isCompleted;
        public float bestCompletionTime;
        public int highScore;
        public List<string> unlockedAchievements;
        public PlanetStats planetStats;
        public List<WaveData> customWaveData;
        public StringIntDictionary resourceInventory;

        public PlanetLevelData()
        {
            unlockedAchievements = new List<string>();
            customWaveData = new List<WaveData>();
            resourceInventory = new StringIntDictionary();
            planetStats = new PlanetStats();
        }
    }

    [Serializable]
    public class PlanetStats
    {
        public float radius;
        public float rotationSpeed;
        public float atmosphereDensity;
        public string biomeType;
        public float resourceRichness;
        public Vector3 position;
        public List<string> specialModifiers;

        public PlanetStats()
        {
            specialModifiers = new List<string>();
        }
    }

    [Serializable]
    public class WaveData
    {
        public int waveNumber;
        public List<EnemySpawnData> enemies;
        public float timeBetweenSpawns;
        public float waveDelay;
        public string specialCondition;

        public WaveData()
        {
            enemies = new List<EnemySpawnData>();
        }
    }

    [Serializable]
    public class EnemySpawnData
    {
        public string enemyType;
        public int count;
        public float health;
        public float speed;
        public string[] modifiers;
    }

    [Serializable]
    public class PlayerProgressData
    {
        public StringIntDictionary turretLevels;
        public StringBoolDictionary unlockedTurrets;
        public StringIntDictionary resourceCounts;
        public List<string> unlockedUpgrades;
        public int totalStars;
        public int totalVictories;
        public float totalPlaytime;

        public PlayerProgressData()
        {
            turretLevels = new StringIntDictionary();
            unlockedTurrets = new StringBoolDictionary();
            resourceCounts = new StringIntDictionary();
            unlockedUpgrades = new List<string>();
        }
    }

}
