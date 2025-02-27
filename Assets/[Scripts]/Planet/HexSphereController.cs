using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class HexSphereController : MonoBehaviour
{
    [Header("Hex Grid Settings")]
    public int gridResolution = 20; // Controls how many hexes are on the sphere
    public float sphereRadius = 10f;
    public Material hexMaterial;
    
    [Header("Visualization Settings")]
    public Color visualizationColor = Color.green;
    [Range(0f, 1f)]
    public float visualizationStrength = 0f;
    
    [Header("Resource Settings")]
    public Texture2D resourceIconAtlas;
    public Texture2D resourceMaskAtlas;
    
    // Internal hex data
    private List<HexTile> hexTiles = new List<HexTile>();
    private ComputeBuffer hexDataBuffer;
    
    // Struct to hold hex data - must match the shader's expected format
    public struct HexTile
    {
        public Vector3 position; // xyz position
        public float data;       // packed data: resource type (8 bits), owner (8 bits), misc flags (16 bits)
        
        public HexTile(Vector3 pos, float hexData = 0)
        {
            position = pos;
            data = hexData;
        }
    }
    
    void Start()
    {
        GenerateHexSphere();
        SetupShaderData();
    }
    
    void OnDestroy()
    {
        // Release compute buffer when done
        if (hexDataBuffer != null)
        {
            hexDataBuffer.Release();
            hexDataBuffer = null;
        }
    }
    
    void Update()
    {
        // Update visualization settings
        if (hexMaterial != null)
        {
            hexMaterial.SetColor("_VisualizationColor", visualizationColor);
            hexMaterial.SetFloat("_VisualizationStrength", visualizationStrength);
            
            // Update any dynamic hex data
            UpdateHexData();
        }
        
        // Handle user interaction if needed
        if (Input.GetMouseButtonDown(0))
        {
            HandleHexSelection();
        }
    }
    
    void GenerateHexSphere()
    {
        hexTiles.Clear();
        
        // Generate a sphere of hexagons using icosphere subdivision
        // This is a simplified approach - a full implementation would use proper hex sphere math
        
        // Start with an icosahedron
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        
        // Generate icosahedron vertices
        float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;
        
        vertices.Add(new Vector3(-1, t, 0).normalized * sphereRadius);
        vertices.Add(new Vector3(1, t, 0).normalized * sphereRadius);
        vertices.Add(new Vector3(-1, -t, 0).normalized * sphereRadius);
        vertices.Add(new Vector3(1, -t, 0).normalized * sphereRadius);
        
        vertices.Add(new Vector3(0, -1, t).normalized * sphereRadius);
        vertices.Add(new Vector3(0, 1, t).normalized * sphereRadius);
        vertices.Add(new Vector3(0, -1, -t).normalized * sphereRadius);
        vertices.Add(new Vector3(0, 1, -t).normalized * sphereRadius);
        
        vertices.Add(new Vector3(t, 0, -1).normalized * sphereRadius);
        vertices.Add(new Vector3(t, 0, 1).normalized * sphereRadius);
        vertices.Add(new Vector3(-t, 0, -1).normalized * sphereRadius);
        vertices.Add(new Vector3(-t, 0, 1).normalized * sphereRadius);
        
        // Initial hex centers from icosahedron vertices
        foreach (Vector3 vertex in vertices)
        {
            hexTiles.Add(new HexTile(vertex));
        }
        
        // Subdivide to desired resolution
        for (int i = 0; i < gridResolution; i++)
        {
            SubdivideHexSphere();
        }
        
        Debug.Log($"Generated hex sphere with {hexTiles.Count} tiles");
    }
    
    void SubdivideHexSphere()
    {
        List<HexTile> newTiles = new List<HexTile>();
        Dictionary<Vector3, bool> tileExists = new Dictionary<Vector3, bool>();
        
        // Track existing tiles to avoid duplicates
        foreach (var tile in hexTiles)
        {
            tileExists[tile.position] = true;
            newTiles.Add(tile);
        }
        
        // For each existing tile, generate surrounding tiles
        for (int i = 0; i < hexTiles.Count; i++)
        {
            Vector3 center = hexTiles[i].position;
            
            // Generate 6 surrounding points at equal angles
            for (int j = 0; j < 6; j++)
            {
                // Create rotation to point j/6 of the way around the tile
                Quaternion rotation = Quaternion.AngleAxis(j * 60f, center);
                
                // Apply rotation to a vector perpendicular to center to get surrounding point
                Vector3 perpendicular = Vector3.Cross(center, center.y != 0 ? Vector3.right : Vector3.up).normalized;
                Vector3 direction = rotation * perpendicular;
                
                // Calculate new point at specified distance
                float distance = sphereRadius * 2f * Mathf.PI / (hexTiles.Count * 0.5f);
                Vector3 newPos = (center + direction * distance).normalized * sphereRadius;
                
                // Round to avoid floating point issues
                newPos = new Vector3(
                    Mathf.Round(newPos.x * 1000f) / 1000f,
                    Mathf.Round(newPos.y * 1000f) / 1000f,
                    Mathf.Round(newPos.z * 1000f) / 1000f
                );
                
                // Add if not already existing
                if (!tileExists.ContainsKey(newPos))
                {
                    tileExists[newPos] = true;
                    newTiles.Add(new HexTile(newPos));
                }
            }
        }
        
        hexTiles = newTiles;
    }
    
    void SetupShaderData()
    {
        if (hexMaterial == null)
        {
            Debug.LogError("Hex material not assigned!");
            return;
        }
        
        // Set up resource textures
        if (resourceIconAtlas != null)
        {
            hexMaterial.SetTexture("_ResourceIconTex", resourceIconAtlas);
        }
        
        if (resourceMaskAtlas != null)
        {
            hexMaterial.SetTexture("_ResourceMaskTex", resourceMaskAtlas);
        }
        
        // Create compute buffer for hex data
        if (hexDataBuffer != null)
        {
            hexDataBuffer.Release();
        }
        
        hexDataBuffer = new ComputeBuffer(hexTiles.Count, sizeof(float) * 4);
        UpdateHexData();
    }
    
    void UpdateHexData()
    {
        if (hexDataBuffer == null || hexMaterial == null)
            return;
            
        // Convert HexTile data to shader-friendly format
        Vector4[] hexData = new Vector4[hexTiles.Count];
        for (int i = 0; i < hexTiles.Count; i++)
        {
            HexTile tile = hexTiles[i];
            hexData[i] = new Vector4(tile.position.x, tile.position.y, tile.position.z, tile.data);
        }
        
        // Update buffer
        hexDataBuffer.SetData(hexData);
        hexMaterial.SetBuffer("_HexData", hexDataBuffer);
    }
    
    void HandleHexSelection()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                // Convert hit point to sphere-relative direction
                Vector3 localHitPoint = hit.point - transform.position;
                Vector3 direction = localHitPoint.normalized;
                
                // Find closest hex
                HexTile closestHex = FindClosestHex(direction);
                
                // Do something with the selected hex
                Debug.Log($"Selected hex at {closestHex.position}");
                
                // Example: Set resource type
                //SetResourceForHex(closestHex, Random.Range(1, 5));
            }
        }
    }
    
    HexTile FindClosestHex(Vector3 direction)
    {
        HexTile closest = hexTiles[0];
        float closestDot = Vector3.Dot(direction, closest.position.normalized);
        
        foreach (var hex in hexTiles)
        {
            float dot = Vector3.Dot(direction, hex.position.normalized);
            if (dot > closestDot)
            {
                closest = hex;
                closestDot = dot;
            }
        }
        
        return closest;
    }
    
    /*// Example method to set resource type for a hex
    public void SetResourceForHex(HexTile hex, int resourceType)
    {
        for (int i = 0; i < hexTiles.Count; i++)
        {
            if (hexTiles[i].position == hex.position)
            {
                // Pack resource type into the lower 8 bits of the data field
                float packedData = (hexTiles[i].data & 0xFFFFFF00) | (resourceType & 0xFF);
                hexTiles[i] = new HexTile(hex.position, packedData);
                break;
            }
        }
        
        // Update shader data
        UpdateHexData();
    }*/
    
    // Example method to highlight hexes based on game mechanics
    public void VisualizeGameMechanics(string mechanicType)
    {
        // Set visualization strength based on what we're showing
        visualizationStrength = 0.7f;
        
        switch (mechanicType)
        {
            case "fertility":
                visualizationColor = new Color(0.2f, 0.8f, 0.3f);
                // Here you would calculate fertility values per hex
                break;
                
            case "resources":
                visualizationColor = new Color(0.9f, 0.7f, 0.1f);
                
                // Example: Highlight hexes with resources
                for (int i = 0; i < hexTiles.Count; i++)
                {
                    // Convert float to int for bitwise operations
                    int intData = (int)hexTiles[i].data;
                    int resourceType = intData & 0xFF; // Extract resource type
                    
                    if (resourceType > 0)
                    {
                        // Do something with resource hexes
                        // e.g., set additional visualization data
                    }
                }
                break;
                
            case "territory":
                visualizationColor = new Color(0.8f, 0.2f, 0.2f);
                // Here you would show territory boundaries
                break;
                
            default:
                visualizationStrength = 0f;
                break;
        }
    }
}