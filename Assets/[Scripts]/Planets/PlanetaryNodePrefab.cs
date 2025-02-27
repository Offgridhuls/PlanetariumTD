using UnityEngine;

namespace Planetarium
{
    /// <summary>
    /// Helper class to set up a planetary node prefab with the required components.
    /// Add this to a prefab that will be used as a node in the planetary expansion system.
    /// </summary>
    public class PlanetaryNodePrefab : MonoBehaviour
    {
        [Header("Node Prefab Settings")]
        [SerializeField] private float nodeSize = 2f;
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Mesh nodeMesh;
        
        [Header("Line Renderer")]
        [SerializeField] private Material connectionMaterial;
        [SerializeField] private float lineWidth = 0.2f;
        
        private void Reset()
        {
            // This is called when the component is first added in the Editor
            // It helps set up the node prefab with default values
            
            // Add required components if they don't exist
            if (GetComponent<PlanetaryNode>() == null)
            {
                gameObject.AddComponent<PlanetaryNode>();
            }
            
            if (GetComponent<PlanetaryNodeVisual>() == null)
            {
                gameObject.AddComponent<PlanetaryNodeVisual>();
            }
            
            // Set up mesh
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            
            if (nodeMesh == null)
            {
                // Default to a sphere if no mesh is specified
                nodeMesh = UnityEngine.Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            }
            meshFilter.sharedMesh = nodeMesh;
            
            // Set up renderer
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<MeshRenderer>();
            }
            
            if (defaultMaterial == null)
            {
                // Create a default material
                defaultMaterial = new Material(Shader.Find("Standard"));
                defaultMaterial.color = Color.gray;
            }
            renderer.sharedMaterial = defaultMaterial;
            
            // Set up collider
            SphereCollider collider = GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<SphereCollider>();
            }
            collider.radius = 0.5f;  // Half of unit size
            
            // Set size
            transform.localScale = Vector3.one * nodeSize;
            
            // Set up line renderer prefab for connections if needed
            SetupConnectionPrefab();
        }
        
        private void SetupConnectionPrefab()
        {
            // Create a new child object for the line renderer
            GameObject connectionObj = new GameObject("ConnectionPrefab");
            connectionObj.transform.SetParent(transform);
            connectionObj.transform.localPosition = Vector3.zero;
            
            // Add line renderer component
            LineRenderer lineRenderer = connectionObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            
            if (connectionMaterial == null)
            {
                // Create a default material for connections
                connectionMaterial = new Material(Shader.Find("Sprites/Default"));
                connectionMaterial.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
            
            lineRenderer.material = connectionMaterial;
            
            // Hide the connection prefab - it will be instantiated when needed
            connectionObj.SetActive(false);
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Update size when changed in the inspector
            transform.localScale = Vector3.one * nodeSize;
            
            // Update any other visual properties that might have changed
            SphereCollider collider = GetComponent<SphereCollider>();
            if (collider != null)
            {
                collider.radius = 0.5f;  // Half of unit size
            }
        }
#endif
    }
} 