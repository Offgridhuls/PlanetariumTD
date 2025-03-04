using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a sphere made of hexagonal tiles, similar to Civilization-style games.
/// Uses a modified icosphere approach to create a grid of hexagons across a sphere.
/// </summary>
public class SphericalHexGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private float sphereRadius = 10f;
    [Range(0, 5)]
    [SerializeField] private int subdivisionLevel = 3; // Higher means more tiles
    [SerializeField] private Material tileMaterial;
    
    [Header("Tile Customization")]
    [SerializeField] private float tileThickness = 0.1f;
    [SerializeField] private Color[] tileColors; // Different colors for tiles
    [SerializeField] private bool useRandomColors = true;
    
    // Used to track created tiles
    private List<HexTile> tiles = new List<HexTile>();
    
    // Used for icosphere generation
    private List<Vector3> icosphereVertices = new List<Vector3>();
    private List<int> icosphereTriangles = new List<int>();
    private Dictionary<long, int> middlePointIndexCache = new Dictionary<long, int>();
    

    private void Start()
    {
        if (tiles.Count == 0)
            GenerateSphere();
    }
    
    /// <summary>
    /// Regenerates the sphere with current settings.
    /// </summary>
    [ContextMenu("Regenerate Sphere")]
    public void GenerateSphere()
    {
        // Initialize lists if they haven't been already
        if (tiles == null)
            tiles = new List<HexTile>();
        
        if (icosphereVertices == null)
            icosphereVertices = new List<Vector3>();
        
        if (icosphereTriangles == null)
            icosphereTriangles = new List<int>();
        
        if (middlePointIndexCache == null)
            middlePointIndexCache = new Dictionary<long, int>();
    
        // Clean up any existing tiles
        foreach (var tile in tiles)
        {
            if (tile != null && tile.gameObject != null)
            {
                if (Application.isPlaying)
                    Destroy(tile.gameObject);
                else
                    DestroyImmediate(tile.gameObject);
            }
        }
        tiles.Clear();
    
        // Reset the working data
        icosphereVertices.Clear();
        icosphereTriangles.Clear();
        middlePointIndexCache.Clear();
    
        // Create and subdivide the icosphere
        CreateIcosphere();
    
        // Convert the icosphere faces to hexagons
        CreateHexagonsFromIcosphere();
    
        // Setup adjacency between tiles
        SetupTileAdjacency();
    
        // Add some portal proximity to certain tiles
        SetupPortalProximity();
    }
    
    
    /// <summary>
    /// Sets up portal proximity values for some tiles
    /// </summary>
    private void SetupPortalProximity()
    {
        if (tiles.Count == 0) return;
    
        // Choose a few random points to be "portals"
        int portalCount = Mathf.Max(1, tiles.Count / 20); // About 5% of tiles are portal-adjacent
        List<int> portalIndices = new List<int>();
    
        for (int i = 0; i < portalCount; i++)
        {
            // Pick a random tile that isn't already a portal
            int attemptCount = 0;
            int randomIndex;
            do
            {
                randomIndex = Random.Range(0, tiles.Count);
                attemptCount++;
            } while (portalIndices.Contains(randomIndex) && attemptCount < 50);
        
            portalIndices.Add(randomIndex);
        }
    
        // For each tile, calculate proximity to nearest portal
        foreach (var tile in tiles)
        {
            float minDistance = float.MaxValue;
        
            foreach (int portalIndex in portalIndices)
            {
                if (portalIndex < tiles.Count)
                {
                    HexTile portalTile = tiles[portalIndex];
                    float distance = Vector3.Distance(tile.GetCenterPosition(), portalTile.GetCenterPosition());
                    minDistance = Mathf.Min(minDistance, distance);
                }
            }
        
            // Normalize distance to 0-1 range (1 = closest to portal)
            float maxDistance = sphereRadius * 2; // Maximum possible distance on sphere
            float proximity = 1f - Mathf.Clamp01(minDistance / maxDistance);
        
            // Set portal proximity on the tile
            tile.SetPortalProximity(proximity);
        }
    }
    
    
    /// <summary>
    /// Creates a base icosphere and subdivides it
    /// </summary>
    private void CreateIcosphere()
    {
        // Step 1: Create the initial icosahedron
        float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;
        
        // Add vertices
        AddVertex(new Vector3(-1, t, 0).normalized * sphereRadius);
        AddVertex(new Vector3(1, t, 0).normalized * sphereRadius);
        AddVertex(new Vector3(-1, -t, 0).normalized * sphereRadius);
        AddVertex(new Vector3(1, -t, 0).normalized * sphereRadius);
        
        AddVertex(new Vector3(0, -1, t).normalized * sphereRadius);
        AddVertex(new Vector3(0, 1, t).normalized * sphereRadius);
        AddVertex(new Vector3(0, -1, -t).normalized * sphereRadius);
        AddVertex(new Vector3(0, 1, -t).normalized * sphereRadius);
        
        AddVertex(new Vector3(t, 0, -1).normalized * sphereRadius);
        AddVertex(new Vector3(t, 0, 1).normalized * sphereRadius);
        AddVertex(new Vector3(-t, 0, -1).normalized * sphereRadius);
        AddVertex(new Vector3(-t, 0, 1).normalized * sphereRadius);
        
        // Add faces (20 triangles of the icosahedron)
        AddFace(0, 11, 5);
        AddFace(0, 5, 1);
        AddFace(0, 1, 7);
        AddFace(0, 7, 10);
        AddFace(0, 10, 11);
        
        AddFace(1, 5, 9);
        AddFace(5, 11, 4);
        AddFace(11, 10, 2);
        AddFace(10, 7, 6);
        AddFace(7, 1, 8);
        
        AddFace(3, 9, 4);
        AddFace(3, 4, 2);
        AddFace(3, 2, 6);
        AddFace(3, 6, 8);
        AddFace(3, 8, 9);
        
        AddFace(4, 9, 5);
        AddFace(2, 4, 11);
        AddFace(6, 2, 10);
        AddFace(8, 6, 7);
        AddFace(9, 8, 1);
        
        // Step 2: Subdivide the icosahedron
        for (int i = 0; i < subdivisionLevel; i++)
        {
            List<int> newTriangles = new List<int>();
            
            for (int j = 0; j < icosphereTriangles.Count; j += 3)
            {
                // Get the three vertices of this face
                int v1 = icosphereTriangles[j];
                int v2 = icosphereTriangles[j + 1];
                int v3 = icosphereTriangles[j + 2];
                
                // Get the midpoints
                int a = GetMiddlePoint(v1, v2);
                int b = GetMiddlePoint(v2, v3);
                int c = GetMiddlePoint(v3, v1);
                
                // Create 4 new triangles
                newTriangles.Add(v1); newTriangles.Add(a); newTriangles.Add(c);
                newTriangles.Add(v2); newTriangles.Add(b); newTriangles.Add(a);
                newTriangles.Add(v3); newTriangles.Add(c); newTriangles.Add(b);
                newTriangles.Add(a); newTriangles.Add(b); newTriangles.Add(c);
            }
            
            icosphereTriangles = newTriangles;
        }
    }
    
    /// <summary>
    /// Creates hex tiles based on vertices from the subdivided icosphere
    /// </summary>
    private void CreateHexagonsFromIcosphere()
    {
        // First, identify dual vertices (centroids of original faces)
        List<Vector3> dualVertices = new List<Vector3>();
        Dictionary<int, List<int>> dualConnections = new Dictionary<int, List<int>>();
        
        // For each face in the original icosphere, create a dual vertex at its centroid
        for (int i = 0; i < icosphereTriangles.Count; i += 3)
        {
            int v1 = icosphereTriangles[i];
            int v2 = icosphereTriangles[i + 1];
            int v3 = icosphereTriangles[i + 2];
            
            // Calculate face centroid
            Vector3 centroid = (icosphereVertices[v1] + icosphereVertices[v2] + icosphereVertices[v3]) / 3f;
            centroid = centroid.normalized * sphereRadius;
            
            int dualIndex = dualVertices.Count;
            dualVertices.Add(centroid);
            
            // Initialize connection list
            dualConnections[dualIndex] = new List<int>();
        }
        
        // For each edge in the original mesh, connect the dual vertices of the adjacent faces
        Dictionary<string, List<int>> edgeToDualVertices = new Dictionary<string, List<int>>();
        
        for (int i = 0; i < icosphereTriangles.Count; i += 3)
        {
            int faceIndex = i / 3;
            int v1 = icosphereTriangles[i];
            int v2 = icosphereTriangles[i + 1];
            int v3 = icosphereTriangles[i + 2];
            
            // For each edge of the face, record connection
            RecordEdgeConnection(edgeToDualVertices, v1, v2, faceIndex);
            RecordEdgeConnection(edgeToDualVertices, v2, v3, faceIndex);
            RecordEdgeConnection(edgeToDualVertices, v3, v1, faceIndex);
        }
        
        // Now connect dual vertices across shared edges
        foreach (var kvp in edgeToDualVertices)
        {
            if (kvp.Value.Count == 2) // Only connect if exactly two faces share this edge
            {
                int dual1 = kvp.Value[0];
                int dual2 = kvp.Value[1];
                
                if (!dualConnections[dual1].Contains(dual2))
                    dualConnections[dual1].Add(dual2);
                    
                if (!dualConnections[dual2].Contains(dual1))
                    dualConnections[dual2].Add(dual1);
            }
        }
        
        // Now create tiles at each original vertex, connecting to the surrounding dual vertices
        HashSet<int> processedVertices = new HashSet<int>();
        
        for (int vertexIndex = 0; vertexIndex < icosphereVertices.Count; vertexIndex++)
        {
            if (processedVertices.Contains(vertexIndex))
                continue;
                
            processedVertices.Add(vertexIndex);
            
            Vector3 center = icosphereVertices[vertexIndex];
            
            // Find all triangles that include this vertex
            List<int> includingFaces = new List<int>();
            for (int i = 0; i < icosphereTriangles.Count; i += 3)
            {
                int v1 = icosphereTriangles[i];
                int v2 = icosphereTriangles[i + 1];
                int v3 = icosphereTriangles[i + 2];
                
                if (v1 == vertexIndex || v2 == vertexIndex || v3 == vertexIndex)
                {
                    includingFaces.Add(i / 3);
                }
            }
            
            // Get the dual vertices (face centroids) for these faces - these will form our tile
            List<Vector3> tileVertices = new List<Vector3>();
            foreach (int faceIndex in includingFaces)
            {
                tileVertices.Add(dualVertices[faceIndex]);
            }
            
            // Only create a tile if we have enough vertices
            if (tileVertices.Count >= 5)
            {
                CreateHexTile(center, tileVertices);
            }
        }
    }
    
    /// <summary>
    /// Helper to record the connection between two faces that share an edge
    /// </summary>
    private void RecordEdgeConnection(Dictionary<string, List<int>> edgeToDualVertices, int v1, int v2, int faceIndex)
    {
        // Create a unique key for this edge (order the vertices to ensure consistency)
        string edgeKey = Mathf.Min(v1, v2) + "_" + Mathf.Max(v1, v2);
        
        if (!edgeToDualVertices.ContainsKey(edgeKey))
        {
            edgeToDualVertices[edgeKey] = new List<int>();
        }
        
        // Record that this face touches this edge
        if (!edgeToDualVertices[edgeKey].Contains(faceIndex))
        {
            edgeToDualVertices[edgeKey].Add(faceIndex);
        }
    }
    
    /// <summary>
    /// Creates a single hex tile at the given center with the surrounding vertices
    /// </summary>
   private void CreateHexTile(Vector3 center, List<Vector3> surroundingVertices)
{
    // Sort vertices around the center
    SortVerticesAroundCenter(center, surroundingVertices);

    // Create regular hexagon vertices
    Vector3[] regularVertices = new Vector3[6];
    List<Vector3> verticesList = new List<Vector3>(6);
    for (int i = 0; i < 6; i++)
    {
        int index = (i * surroundingVertices.Count / 6) % surroundingVertices.Count;
        regularVertices[i] = surroundingVertices[index];
        verticesList.Add(surroundingVertices[index]);
    }

    // Create GameObject and add components
    GameObject hexObj = new GameObject("HexTile");
    hexObj.transform.SetParent(transform);
    
    // Add the HexTile component
    HexTile hexTileComponent = hexObj.AddComponent<HexTile>();
    
    MeshFilter meshFilter = hexObj.AddComponent<MeshFilter>();
    MeshRenderer meshRenderer = hexObj.AddComponent<MeshRenderer>();

    // Create mesh with improved UV mapping
    Mesh mesh = CreateHexagonMesh(center, regularVertices);
    meshFilter.mesh = mesh;

    // Initialize the HexTile component with proper data
    int tileIndex = tiles.Count;
    hexTileComponent.Initialize(tileIndex, center, verticesList);

    // Handle material
    if (Application.isPlaying)
    {
        meshRenderer.material = new Material(tileMaterial);
        if (tileColors != null && tileColors.Length > 0)
        {
            int terrainType = useRandomColors ? Random.Range(0, tileColors.Length) : 0;
            meshRenderer.material.color = tileColors[terrainType];
            hexTileComponent.SetTerrainType(terrainType);
        }
    }
    else
    {
        meshRenderer.sharedMaterial = tileMaterial;
        if (tileColors != null && tileColors.Length > 0)
        {
            int terrainType = useRandomColors ? Random.Range(0, tileColors.Length) : 0;
            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            propBlock.SetColor("_Color", tileColors[terrainType]);
            meshRenderer.SetPropertyBlock(propBlock);
            hexTileComponent.SetTerrainType(terrainType);
        }
    }

    // Add the tile to our list
    tiles.Add(hexTileComponent);
}
    
    
    /// <summary>
    /// Sets up adjacency relationships between tiles after all tiles are created
    /// </summary>
    private void SetupTileAdjacency()
    {
        // For each tile
        for (int i = 0; i < tiles.Count; i++)
        {
            HexTile currentTile = tiles[i];
            List<HexTile> adjacentTiles = new List<HexTile>();
        
            // Check each other tile for adjacency
            for (int j = 0; j < tiles.Count; j++)
            {
                if (i == j) continue; // Skip self
            
                HexTile otherTile = tiles[j];
            
                // Tiles are adjacent if they share at least one vertex
                bool isAdjacent = false;
                List<Vector3> currentVertices = currentTile.GetVertices();
                List<Vector3> otherVertices = otherTile.GetVertices();
            
                if (currentVertices != null && otherVertices != null)
                {
                    foreach (Vector3 v1 in currentVertices)
                    {
                        foreach (Vector3 v2 in otherVertices)
                        {
                            // If vertices are very close, consider them shared
                            if (Vector3.Distance(v1, v2) < 0.01f * sphereRadius)
                            {
                                isAdjacent = true;
                                break;
                            }
                        }
                        if (isAdjacent) break;
                    }
                }
            
                if (isAdjacent)
                {
                    adjacentTiles.Add(otherTile);
                }
            }
        
            // Set adjacent tiles
            currentTile.SetAdjacentTiles(adjacentTiles);
        }
    
        // Calculate strategic values based on adjacency
        foreach (var tile in tiles)
        {
            tile.CalculateStrategicValue();
        }
    }
    
    
    
    
    private Mesh CreateHexagonMesh(Vector3 center, Vector3[] vertices)
    {
        Mesh mesh = new Mesh();
    
        // Create vertex array including center point
        Vector3[] meshVertices = new Vector3[7];
        meshVertices[0] = center;
        for (int i = 0; i < 6; i++)
        {
            meshVertices[i + 1] = vertices[i];
        }

        // Create triangles (fan formation)
        int[] triangles = new int[18]; // 6 triangles * 3 vertices
        for (int i = 0; i < 6; i++)
        {
            int baseIndex = i * 3;
            triangles[baseIndex] = 0; // center
            triangles[baseIndex + 1] = i + 1;
            triangles[baseIndex + 2] = ((i + 1) % 6) + 1;
        }

        // Create UVs
        Vector2[] uvs = new Vector2[7];
        uvs[0] = new Vector2(0.5f, 0.5f); // center
        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.PI * 2f / 6f;
            uvs[i + 1] = new Vector2(
                0.5f + Mathf.Cos(angle) * 0.5f,
                0.5f + Mathf.Sin(angle) * 0.5f
            );
        }

        // Assign to mesh
        mesh.vertices = meshVertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
    
    
    /// <summary>
    /// Helper method to add a vertex to the icosphere
    /// </summary>
    private int AddVertex(Vector3 vertex)
    {
        icosphereVertices.Add(vertex);
        return icosphereVertices.Count - 1;
    }
    
    /// <summary>
    /// Helper method to add a triangle face to the icosphere
    /// </summary>
    private void AddFace(int v1, int v2, int v3)
    {
        icosphereTriangles.Add(v1);
        icosphereTriangles.Add(v2);
        icosphereTriangles.Add(v3);
    }
    
    /// <summary>
    /// Gets or creates a vertex at the middle point of two existing vertices
    /// </summary>
    private int GetMiddlePoint(int v1, int v2)
    {
        // Check if we've already created this middle point
        long smallerIndex = Mathf.Min(v1, v2);
        long greaterIndex = Mathf.Max(v1, v2);
        long key = (smallerIndex << 32) + greaterIndex;
        
        int ret;
        if (middlePointIndexCache.TryGetValue(key, out ret))
        {
            return ret;
        }
        
        // If not in cache, calculate the middle point
        Vector3 point1 = icosphereVertices[v1];
        Vector3 point2 = icosphereVertices[v2];
        Vector3 middle = (point1 + point2) / 2.0f;
        
        // Normalize to project onto the sphere
        middle = middle.normalized * sphereRadius;
        
        // Add the new vertex
        int newIndex = AddVertex(middle);
        
        // Add to cache
        middlePointIndexCache.Add(key, newIndex);
        return newIndex;
    }
    
    /// <summary>
    /// Sorts vertices in a circular order around a center point
    /// </summary>
    private void SortVerticesAroundCenter(Vector3 center, List<Vector3> vertices)
    {
        // Get the normal at the center point (pointing outward from sphere)
        Vector3 normal = center.normalized;
        
        // Create a reference direction (any perpendicular to normal)
        Vector3 refDirection = Vector3.Cross(normal, normal.y > 0.9f ? Vector3.right : Vector3.up).normalized;
        
        // Sort vertices by angle around the center
        vertices.Sort((a, b) => {
            // Project both points onto the tangent plane
            Vector3 aFlat = a - center;
            Vector3 bFlat = b - center;
            aFlat = aFlat - Vector3.Dot(aFlat, normal) * normal;
            bFlat = bFlat - Vector3.Dot(bFlat, normal) * normal;
            
            // Calculate signed angles from the reference direction
            float angleA = Vector3.SignedAngle(refDirection, aFlat, normal);
            float angleB = Vector3.SignedAngle(refDirection, bFlat, normal);
            
            // Ensure positive angles
            if (angleA < 0) angleA += 360f;
            if (angleB < 0) angleB += 360f;
                
            return angleA.CompareTo(angleB);
        });
    }
    
    /// <summary>
    /// Gets a tile at the given world position (for gameplay interaction)
    /// </summary>
    public HexTile GetTileAtPosition(Vector3 position)
    {
        // Find the closest tile center to the given position
        HexTile closestTile = null;
        float closestDistance = float.MaxValue;
        
        foreach (var tile in tiles)
        {
            float distance = Vector3.Distance(position, tile.centerPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTile = tile;
            }
        }
        
        return closestTile;
    }
}