using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls individual hexagonal tiles for a tower defense game with Civilization-like mechanics.
/// Handles resource generation, strategic placement, hotspots, and turret management.
/// </summary>
public class HexTile : MonoBehaviour
{
    [Header("Identification")]
    [SerializeField] private int tileIndex;
    [SerializeField] public Vector3 centerPosition;
    [SerializeField] private List<Vector3> vertices = new List<Vector3>();
    
    [Header("Resource Properties")]
    [SerializeField] private ResourceType resourceType = ResourceType.None;
    [SerializeField] private int resourceTier = 0; // 0-3, higher tiers give more resources
    [SerializeField] private float resourceGenerationRate = 1.0f;
    [SerializeField] private float resourceAmount = 0f;
    
    [Header("Strategic Properties")]
    [SerializeField] private float portalProximity = 0f; // 0-1 value, 1 = closest to portal
    [SerializeField] private bool isHotspot = false;
    [SerializeField] private int hotspotTier = 0; // 0-3, higher tiers give more bonuses
    [SerializeField] private TilePriority priority = TilePriority.Medium;
    
    [Header("Tower Defense Properties")]
    [SerializeField] private bool isOccupied = false;
    [SerializeField] private TurretType turretType = TurretType.None;
    [SerializeField] private int turretCapacity = 1; // Base capacity
    [SerializeField] private int currentTurretCount = 0;
    [SerializeField] private List<GameObject> placedTurrets = new List<GameObject>();
    
    // References
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock materialProps;
    private List<HexTile> adjacentTiles = new List<HexTile>();
    
    // Resource generation timer
    private float resourceTimer = 0f;
    
    // Add this method to your HexTile class
    public List<Vector3> GetVertices() => vertices;
    
    
    // Enums
    public enum ResourceType
    {
        None,
        Energy,
        Minerals,
        Technology,
        Strategic
    }
    
    public enum TurretType
    {
        None,
        Basic,
        Cannon,
        Laser,
        Shield,
        Special
    }
    
    public enum TilePriority
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    // Resource value multipliers
    private static readonly Dictionary<ResourceType, float> ResourceValues = new Dictionary<ResourceType, float>
    {
        { ResourceType.None, 0f },
        { ResourceType.Energy, 1.0f },
        { ResourceType.Minerals, 0.8f },
        { ResourceType.Technology, 0.5f },
        { ResourceType.Strategic, 0.3f }
    };
    
    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        
        // Create material property block for shader control
        materialProps = new MaterialPropertyBlock();
        if (meshRenderer != null)
        {
            meshRenderer.GetPropertyBlock(materialProps);
        }
    }
    
    void Start()
    {
        UpdateVisuals();
    }
    
    void Update()
    {
        // Generate resources over time
        if (resourceType != ResourceType.None && !isOccupied)
        {
            resourceTimer += Time.deltaTime;
            if (resourceTimer >= 1.0f)
            {
                GenerateResource();
                resourceTimer = 0f;
            }
        }
    }
    
    /// <summary>
    /// Initialize the hex tile with basic information
    /// </summary>
    public void Initialize(int index, Vector3 center, List<Vector3> tileVertices)
    {
        tileIndex = index;
        centerPosition = center;
        vertices = new List<Vector3>(tileVertices);
        
       // transform.position = center;
    }
    
    /// <summary>
    /// Sets the terrain type of the tile, which influences resource generation
    /// </summary>
    public void SetTerrainType(int terrainType)
    {
        // Map terrain type to a resource type
        switch (terrainType % 5)
        {
            case 0:
                resourceType = ResourceType.None;
                break;
            case 1:
                resourceType = ResourceType.Energy;
                break;
            case 2:
                resourceType = ResourceType.Minerals;
                break;
            case 3:
                resourceType = ResourceType.Technology;
                break;
            case 4:
                resourceType = ResourceType.Strategic;
                break;
        }
        
        // Higher terrain types have better resource tiers
        resourceTier = Mathf.Min(terrainType / 5, 3);
        
        UpdateResourceGenerationRate();
        UpdateVisuals();
    }
    
    /// <summary>
    /// Sets the adjacency relationship with other tiles
    /// </summary>
    public void SetAdjacentTiles(List<HexTile> tiles)
    {
        adjacentTiles = tiles;
        CalculateStrategicValue();
    }
    
    /// <summary>
    /// Updates the resource generation rate based on type and tier
    /// </summary>
    private void UpdateResourceGenerationRate()
    {
        if (resourceType == ResourceType.None)
        {
            resourceGenerationRate = 0f;
            return;
        }
        
        // Base rate depends on resource type
        float baseRate = ResourceValues[resourceType];
        
        // Tier multiplier
        float tierMultiplier = 1f + (resourceTier * 0.25f);
        
        // Hotspot bonus
        float hotspotMultiplier = isHotspot ? 1f + (hotspotTier * 0.2f) : 1f;
        
        // Portal proximity risk/reward factor
        float portalMultiplier = 1f + (portalProximity * 0.5f);
        
        // Calculate final rate
        resourceGenerationRate = baseRate * tierMultiplier * hotspotMultiplier * portalMultiplier;
    }
    
    /// <summary>
    /// Generates resources based on the tile's properties
    /// </summary>
    private void GenerateResource()
    {
        if (resourceType == ResourceType.None) return;
        
        // Add resources based on generation rate
        resourceAmount += resourceGenerationRate;
        
        // Update visuals to show resource accumulation
        UpdateVisuals();
    }
    
    /// <summary>
    /// Harvests and returns the accumulated resources
    /// </summary>
    public float HarvestResources()
    {
        float amount = resourceAmount;
        resourceAmount = 0f;
        UpdateVisuals();
        return amount;
    }
    
    /// <summary>
    /// Calculates the strategic value of this tile based on position,
    /// adjacency, and other factors
    /// </summary>
    public void CalculateStrategicValue()
    {
        if (adjacentTiles.Count == 0) return;
        
        // Count adjacent resource types
        Dictionary<ResourceType, int> adjacentResources = new Dictionary<ResourceType, int>();
        foreach (var adjacent in adjacentTiles)
        {
            if (adjacent.resourceType != ResourceType.None)
            {
                if (!adjacentResources.ContainsKey(adjacent.resourceType))
                {
                    adjacentResources[adjacent.resourceType] = 0;
                }
                adjacentResources[adjacent.resourceType]++;
            }
        }
        
        // Calculate resource diversity bonus
        int resourceTypes = adjacentResources.Count;
        
        // Determine if this is a hotspot
        bool shouldBeHotspot = false;
        int newHotspotTier = 0;
        
        // Criteria for hotspots:
        // 1. Diverse resources nearby (2+ types)
        if (resourceTypes >= 2)
        {
            shouldBeHotspot = true;
            newHotspotTier = Mathf.Clamp(resourceTypes - 1, 0, 2);
        }
        
        // 2. Near portal (strategic defensive position)
        if (portalProximity > 0.5f)
        {
            shouldBeHotspot = true;
            newHotspotTier = Mathf.Max(newHotspotTier, Mathf.FloorToInt(portalProximity * 3));
        }
        
        // 3. Resource-rich tile itself
        if (resourceType == ResourceType.Strategic || resourceTier >= 2)
        {
            shouldBeHotspot = true;
            newHotspotTier = Mathf.Max(newHotspotTier, resourceTier);
        }
        
        // Update hotspot status if changed
        if (isHotspot != shouldBeHotspot || hotspotTier != newHotspotTier)
        {
            isHotspot = shouldBeHotspot;
            hotspotTier = newHotspotTier;
            
            // Update turret capacity based on hotspot tier
            turretCapacity = 1 + hotspotTier;
            
            // Update resource generation now that hotspot status changed
            UpdateResourceGenerationRate();
            
            // Update visuals
            UpdateVisuals();
        }
    }
    
    /// <summary>
    /// Sets the tile's proximity to the nearest portal
    /// </summary>
    public void SetPortalProximity(float proximity)
    {
        portalProximity = Mathf.Clamp01(proximity);
        CalculateStrategicValue();
        UpdateVisuals();
    }
    
    /// <summary>
    /// Updates the visual appearance of the tile based on its properties
    /// </summary>
    public void UpdateVisuals()
    {
        if (meshRenderer == null) return;
        
        // Get current material properties
        meshRenderer.GetPropertyBlock(materialProps);
        
        // Set main color based on resource type
        materialProps.SetColor("_MainColor", GetResourceTypeColor());
        
        // Set hotspot intensity and color
        float hotspotIntensity = isHotspot ? 0.2f + (0.2f * hotspotTier) : 0f;
        materialProps.SetFloat("_HotspotIntensity", hotspotIntensity);
        
        // Portal proximity effect (orange to red gradient)
        Color hotspotColor = Color.Lerp(new Color(1f, 0.7f, 0f), new Color(1f, 0.3f, 0f), portalProximity);
        materialProps.SetColor("_HotspotColor", hotspotColor);
        
        // Set pulse speed based on resource amount
        float pulseSpeed = 1f + Mathf.Clamp01(resourceAmount / 10f) * 2f;
        materialProps.SetFloat("_PulseSpeed", pulseSpeed);
        
        // Apply the properties to the renderer
        meshRenderer.SetPropertyBlock(materialProps);
    }
    
    /// <summary>
    /// Gets the color associated with the resource type
    /// </summary>
    private Color GetResourceTypeColor()
    {
        switch (resourceType)
        {
            case ResourceType.Energy:
                return new Color(0.2f, 0.6f, 1f); // Blue
            case ResourceType.Minerals:
                return new Color(0.6f, 0.3f, 0.8f); // Purple
            case ResourceType.Technology:
                return new Color(0.2f, 0.8f, 0.4f); // Green
            case ResourceType.Strategic:
                return new Color(1f, 0.6f, 0.2f); // Orange
            default:
                return new Color(0.7f, 0.7f, 0.7f); // Gray
        }
    }
    
    /// <summary>
    /// Attempts to place a turret on this tile
    /// </summary>
    public bool PlaceTurret(TurretType type, GameObject turretPrefab)
    {
        // Check if there's room for another turret
        if (currentTurretCount >= turretCapacity)
        {
            Debug.Log($"Tile {tileIndex} at capacity ({currentTurretCount}/{turretCapacity})");
            return false;
        }
        
        // If this is the first turret, set the type
        if (turretType == TurretType.None)
        {
            turretType = type;
        }
        
        // Instantiate turret
        if (turretPrefab != null)
        {
            GameObject turret = Instantiate(turretPrefab, transform.position, Quaternion.identity, transform);
            
            // Position slightly above the surface
            turret.transform.position = centerPosition + centerPosition.normalized * 0.1f;
            
            // Orient to face outward from the sphere
            turret.transform.up = centerPosition.normalized;
            
            // Add to the list of placed turrets
            placedTurrets.Add(turret);
        }
        
        // Update counters
        currentTurretCount++;
        isOccupied = currentTurretCount >= turretCapacity;
        
        // Update visuals
        UpdateVisuals();
        
        return true;
    }
    
    /// <summary>
    /// Removes a turret from this tile
    /// </summary>
    public bool RemoveTurret()
    {
        if (currentTurretCount <= 0)
        {
            return false;
        }
        
        // Remove the last placed turret
        if (placedTurrets.Count > 0)
        {
            GameObject turret = placedTurrets[placedTurrets.Count - 1];
            placedTurrets.RemoveAt(placedTurrets.Count - 1);
            
            if (turret != null)
            {
                Destroy(turret);
            }
        }
        
        // Update counters
        currentTurretCount--;
        
        // If no turrets left, reset the type
        if (currentTurretCount == 0)
        {
            turretType = TurretType.None;
            isOccupied = false;
        }
        else
        {
            isOccupied = currentTurretCount >= turretCapacity;
        }
        
        // Update visuals
        UpdateVisuals();
        
        return true;
    }
    
    /// <summary>
    /// Sets the selected state of the tile
    /// </summary>
    public void SetSelected(bool selected)
    {
        meshRenderer.GetPropertyBlock(materialProps);
        materialProps.SetFloat("_IsSelected", selected ? 1f : 0f);
        meshRenderer.SetPropertyBlock(materialProps);
    }
    
    /// <summary>
    /// Sets the priority level of the tile
    /// </summary>
    public void SetPriority(TilePriority newPriority)
    {
        priority = newPriority;
        UpdateVisuals();
    }
    
    // Getters
    public int GetTileIndex() => tileIndex;
    public Vector3 GetCenterPosition() => centerPosition;
    public ResourceType GetResourceType() => resourceType;
    public int GetResourceTier() => resourceTier;
    public float GetResourceAmount() => resourceAmount;
    public float GetResourceGenerationRate() => resourceGenerationRate;
    public bool IsHotspot() => isHotspot;
    public int GetHotspotTier() => hotspotTier;
    public int GetTurretCapacity() => turretCapacity;
    public int GetCurrentTurretCount() => currentTurretCount;
    public bool IsOccupied() => isOccupied;
    public TurretType GetTurretType() => turretType;
    public float GetPortalProximity() => portalProximity;
    public TilePriority GetPriority() => priority;
    public List<HexTile> GetAdjacentTiles() => adjacentTiles;
    
    // For visualization in the Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centerPosition, 0.1f);
        
        if (vertices != null && vertices.Count > 0)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                int nextIndex = (i + 1) % vertices.Count;
                Gizmos.DrawLine(vertices[i], vertices[nextIndex]);
            }
        }
        
        // Visualize hotspot status
        if (isHotspot)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawSphere(centerPosition, 0.1f + 0.05f * hotspotTier);
        }
    }
}