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
    
    [System.Serializable]
    public class HexTile
    {
        public GameObject gameObject;
        public Vector3 centerPosition;
        public List<Vector3> vertices = new List<Vector3>();
        public int terrainType = 0; // 0: ocean, 1: plains, 2: mountains, etc.
    }
    
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
        // Clean up any existing tiles
        foreach (var tile in tiles)
        {
            if (tile.gameObject != null)
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
        // Create new tile
        HexTile tile = new HexTile();
        tile.centerPosition = center;
        
        // Assign a random terrain type
        if (tileColors != null && tileColors.Length > 0)
        {
            tile.terrainType = useRandomColors ? Random.Range(0, tileColors.Length) : 0;
        }
        
        // Sort the vertices around the center
        SortVerticesAroundCenter(center, surroundingVertices);
        
        // For more regular hexagons, take vertices at equal angles
        List<Vector3> regularVertices = new List<Vector3>();
        int numSides = Mathf.Min(surroundingVertices.Count, 6);
        
        for (int i = 0; i < numSides; i++)
        {
            int index = (i * surroundingVertices.Count / numSides) % surroundingVertices.Count;
            regularVertices.Add(surroundingVertices[index]);
        }
        
        tile.vertices = regularVertices;
        
        // Create the GameObject and mesh
        GameObject hexObj = new GameObject("HexTile");
        hexObj.transform.SetParent(transform);
        
        MeshFilter meshFilter = hexObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = hexObj.AddComponent<MeshRenderer>();
        
        // Create the mesh
        Mesh mesh = new Mesh();
        
        // Create top and bottom vertices for the extruded hex
        List<Vector3> meshVertices = new List<Vector3>();
        
        // Add top vertices (at sphere radius)
        meshVertices.Add(center);
        int centerTopIndex = 0;
        
        foreach (Vector3 vertex in tile.vertices)
        {
            meshVertices.Add(vertex);
        }
        
        // Add bottom vertices (pushed inward by tileThickness)
        Vector3 inwardDirection = -center.normalized;
        meshVertices.Add(center + inwardDirection * tileThickness);
        int centerBottomIndex = meshVertices.Count - 1;
        
        foreach (Vector3 vertex in tile.vertices)
        {
            // Pull vertices slightly inward to prevent overlap
            Vector3 pullDirection = (center - vertex).normalized * 0.02f * sphereRadius;
            Vector3 adjustedVertex = vertex + pullDirection;
            
            // Now push inward for thickness
            meshVertices.Add(adjustedVertex + inwardDirection * tileThickness);
        }
        
        // Create triangles
        List<int> triangles = new List<int>();
        
        // Top face
        for (int i = 0; i < tile.vertices.Count; i++)
        {
            int nextI = (i + 1) % tile.vertices.Count;
            triangles.Add(centerTopIndex);
            triangles.Add(i + 1);
            triangles.Add(nextI + 1);
        }
        
        // Bottom face (reversed winding order)
        for (int i = 0; i < tile.vertices.Count; i++)
        {
            int nextI = (i + 1) % tile.vertices.Count;
            triangles.Add(centerBottomIndex);
            triangles.Add(nextI + tile.vertices.Count + 1);
            triangles.Add(i + tile.vertices.Count + 1);
        }
        
        // Side faces
        for (int i = 0; i < tile.vertices.Count; i++)
        {
            int nextI = (i + 1) % tile.vertices.Count;
            
            // Upper triangle
            triangles.Add(i + 1);
            triangles.Add(i + tile.vertices.Count + 1);
            triangles.Add(nextI + 1);
            
            // Lower triangle
            triangles.Add(nextI + 1);
            triangles.Add(i + tile.vertices.Count + 1);
            triangles.Add(nextI + tile.vertices.Count + 1);
        }
        
        // Assign to mesh
        mesh.vertices = meshVertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        
        // Assign to GameObject
        meshFilter.mesh = mesh;
        
        // Handle material assignment to prevent leaks
        if (Application.isPlaying)
        {
            meshRenderer.material = new Material(tileMaterial);
            if (tileColors != null && tileColors.Length > tile.terrainType)
            {
                meshRenderer.material.color = tileColors[tile.terrainType];
            }
        }
        else
        {
            meshRenderer.sharedMaterial = tileMaterial;
            
            // For edit mode color variation, use MaterialPropertyBlock
            if (tileColors != null && tileColors.Length > tile.terrainType)
            {
                MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
                propBlock.SetColor("_Color", tileColors[tile.terrainType]);
                meshRenderer.SetPropertyBlock(propBlock);
            }
        }
        
        // Store in our tile list
        tile.gameObject = hexObj;
        tiles.Add(tile);
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