using UnityEngine;
using System;
using Planetarium.Audio;

namespace Planetarium
{
    [Serializable]
    public class SceneContext
    {
        // Core Systems
        
        public GameStateManager GameState;
        
        public EnemyManager EnemyManager;
        
        public TurretManager TurretManager;
        
        public ResourceManager ResourceManager;
        
        public ResourceInventory ResourceInventory;
        
        // Camera and Input
        public Camera MainCamera;
        public CursorController CursorController;
        
        // Audio
        public SoundManager Audio;
        
        // Planet
        
        public PlanetBase CurrentPlanet;
        
        // Wave Configuration
        
        public WaveConfiguration WaveConfig;
        
        // UI References
        public Canvas GameplayCanvas;

        //Camera control service
        public CameraControlService CameraControlService;
        
        // Game Settings
        [HideInInspector]
        public bool IsGamePaused;
        [HideInInspector]
        public bool HasInput = true;
        [HideInInspector]
        public bool IsVisible = true;
        
        
        
    }
}
