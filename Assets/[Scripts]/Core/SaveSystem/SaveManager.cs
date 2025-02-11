using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planetarium.SaveSystem
{
    public class SaveManager : SceneService
    {
        private const string SAVE_DIRECTORY = "Saves";
        private const string SAVE_EXTENSION = ".psave";
        private const int MAX_AUTOSAVES = 3;
        private const float AUTOSAVE_INTERVAL = 300f; // 5 minutes
        private const string ENCRYPTION_KEY = "PlanetariumTD2025"; // Simple XOR encryption key

        private GameSaveData currentSave;
        private float lastAutosaveTime;
        private string currentSaveSlot;
        
        public GameSaveData CurrentSave => currentSave;
        public bool HasActiveSave => currentSave != null;

        protected override void OnInitialize()
        {
            // Create save directory if it doesn't exist
            string savePath = GetSaveDirectoryPath();
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            // Load most recent autosave if it exists
            TryLoadMostRecentAutosave();
        }

        protected override void OnTick()
        {
            // Handle autosaving
            if (HasActiveSave && Time.time - lastAutosaveTime >= AUTOSAVE_INTERVAL)
            {
                CreateAutosave();
                lastAutosaveTime = Time.time;
            }
        }

        public void CreateNewGame(string playerName)
        {
            currentSave = new GameSaveData
            {
                playerName = playerName,
                currency = 1000, // Starting currency
                playtime = 0,
                lastSaveTime = DateTime.Now.ToString("o")
            };

            // Add initial solar system
            var startingSystem = CreateStartingSolarSystem();
            currentSave.unlockedSystems.Add(startingSystem);
            currentSave.currentSystem = startingSystem.systemId;

            // Save immediately
            SaveGame("NewGame");
        }

        public async Task<bool> SaveGame(string slotName)
        {
            if (!HasActiveSave) return false;

            try
            {
                currentSave.lastSaveTime = DateTime.Now.ToString("o");
                currentSaveSlot = slotName;

                string savePath = GetSaveFilePath(slotName);
                string jsonData = JsonUtility.ToJson(currentSave, true);
                
                // Encrypt the data
                string encryptedData = JsonHelper.EncryptDecrypt(jsonData, ENCRYPTION_KEY);
                
                await File.WriteAllTextAsync(savePath, encryptedData);
                Debug.Log($"Game saved successfully to {savePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save game: {e.Message}");
                return false;
            }
        }

        public async Task<bool> LoadGame(string slotName)
        {
            try
            {
                string savePath = GetSaveFilePath(slotName);
                if (!File.Exists(savePath))
                {
                    Debug.LogWarning($"Save file not found: {savePath}");
                    return false;
                }

                string encryptedData = await File.ReadAllTextAsync(savePath);
                string jsonData = JsonHelper.EncryptDecrypt(encryptedData, ENCRYPTION_KEY);
                
                currentSave = JsonUtility.FromJson<GameSaveData>(jsonData);
                currentSaveSlot = slotName;

                Debug.Log($"Game loaded successfully from {savePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load game: {e.Message}");
                return false;
            }
        }

        public List<SaveFileInfo> GetAvailableSaves()
        {
            List<SaveFileInfo> saves = new List<SaveFileInfo>();
            string savePath = GetSaveDirectoryPath();

            if (!Directory.Exists(savePath)) return saves;

            foreach (string file in Directory.GetFiles(savePath, $"*{SAVE_EXTENSION}"))
            {
                try
                {
                    string encryptedData = File.ReadAllText(file);
                    string jsonData = JsonHelper.EncryptDecrypt(encryptedData, ENCRYPTION_KEY);
                    var saveData = JsonUtility.FromJson<GameSaveData>(jsonData);
                    
                    saves.Add(new SaveFileInfo
                    {
                        fileName = Path.GetFileNameWithoutExtension(file),
                        playerName = saveData.playerName,
                        lastSaveTime = saveData.GetLastSaveTime(),
                        playtime = saveData.playtime,
                        unlockedSystems = saveData.unlockedSystems.Count
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error reading save file {file}: {e.Message}");
                }
            }

            saves.Sort((a, b) => b.lastSaveTime.CompareTo(a.lastSaveTime));
            return saves;
        }

        public void DeleteSave(string slotName)
        {
            try
            {
                string savePath = GetSaveFilePath(slotName);
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                    Debug.Log($"Save file deleted: {savePath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save file: {e.Message}");
            }
        }

        private void CreateAutosave()
        {
            string autoSaveName = $"Autosave_{DateTime.Now:yyyyMMdd_HHmmss}";
            SaveGame(autoSaveName);

            // Clean up old autosaves
            CleanupOldAutosaves();
        }

        private void CleanupOldAutosaves()
        {
            var saves = GetAvailableSaves();
            var autosaves = saves.FindAll(s => s.fileName.StartsWith("Autosave_"));
            
            if (autosaves.Count > MAX_AUTOSAVES)
            {
                autosaves.Sort((a, b) => b.lastSaveTime.CompareTo(a.lastSaveTime));
                for (int i = MAX_AUTOSAVES; i < autosaves.Count; i++)
                {
                    DeleteSave(autosaves[i].fileName);
                }
            }
        }

        private void TryLoadMostRecentAutosave()
        {
            var saves = GetAvailableSaves();
            var autosaves = saves.FindAll(s => s.fileName.StartsWith("Autosave_"));
            
            if (autosaves.Count > 0)
            {
                autosaves.Sort((a, b) => b.lastSaveTime.CompareTo(a.lastSaveTime));
                LoadGame(autosaves[0].fileName);
            }
        }

        private string GetSaveDirectoryPath()
        {
            return Path.Combine(Application.persistentDataPath, SAVE_DIRECTORY);
        }

        private string GetSaveFilePath(string slotName)
        {
            return Path.Combine(GetSaveDirectoryPath(), $"{slotName}{SAVE_EXTENSION}");
        }

        private SolarSystemData CreateStartingSolarSystem()
        {
            return new SolarSystemData
            {
                systemId = "sol_system",
                systemName = "Solar System",
                isUnlocked = true,
                position = Vector3.zero,
                difficulty = 1.0f,
                planets = new List<PlanetLevelData>
                {
                    CreateStartingPlanet()
                },
                availableTurrets = new List<string> { "basic_turret", "missile_turret" },
                availableResources = new List<string> { "credits", "metal", "energy" }
            };
        }

        private PlanetLevelData CreateStartingPlanet()
        {
            return new PlanetLevelData
            {
                planetId = "earth",
                planetName = "Earth",
                isUnlocked = true,
                planetStats = new PlanetStats
                {
                    radius = 100f,
                    rotationSpeed = 1f,
                    atmosphereDensity = 1f,
                    biomeType = "temperate",
                    resourceRichness = 1f,
                    position = Vector3.zero,
                    specialModifiers = new List<string>()
                },
                customWaveData = CreateStartingWaves()
            };
        }

        private List<WaveData> CreateStartingWaves()
        {
            return new List<WaveData>
            {
                new WaveData
                {
                    waveNumber = 1,
                    enemies = new List<EnemySpawnData>
                    {
                        new EnemySpawnData
                        {
                            enemyType = "basic_enemy",
                            count = 10,
                            health = 100f,
                            speed = 1f,
                            modifiers = new string[0]
                        }
                    },
                    timeBetweenSpawns = 1f,
                    waveDelay = 5f
                }
            };
        }
    }

    public class SaveFileInfo
    {
        public string fileName;
        public string playerName;
        public DateTime lastSaveTime;
        public float playtime;
        public int unlockedSystems;
    }
}
