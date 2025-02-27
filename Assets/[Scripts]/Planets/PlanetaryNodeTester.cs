using UnityEngine;

namespace Planetarium
{
    /// <summary>
    /// Simple test script to verify the planetary node implementation.
    /// Attach this to a GameObject in the scene to test the node visuals.
    /// </summary>
    public class PlanetaryNodeTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private PlanetBase planet;
        [SerializeField] private int testNodeCount = 10;
        [SerializeField] private float nodeSpacing = 10f;
        
        [Header("Materials")]
        [SerializeField] private Material lockedMaterial;
        [SerializeField] private Material availableMaterial;
        [SerializeField] private Material ownedMaterial;
        
        private PlanetaryNode[] testNodes;
        
        private void Start()
        {
            if (planet == null)
            {
                planet = FindFirstObjectByType<PlanetBase>();
                if (planet == null)
                {
                    Debug.LogError("No planet found in the scene!");
                    return;
                }
            }
            
            CreateTestNodes();
        }
        
        private void CreateTestNodes()
        {
            testNodes = new PlanetaryNode[testNodeCount];
            
            // Create a container for the test nodes
            Transform container = new GameObject("TestNodes").transform;
            container.SetParent(transform);
            
            float planetRadius = planet.GetPlanetRadius();
            Vector3 planetCenter = planet.transform.position;
            
            // Use the Fibonacci sphere algorithm for even distribution
            float goldenRatio = (1 + Mathf.Sqrt(5)) / 2;
            float angleIncrement = 2 * Mathf.PI * goldenRatio;
            
            for (int i = 0; i < testNodeCount; i++)
            {
                // Calculate position on sphere
                float t = (float)i / testNodeCount;
                float inclination = Mathf.Acos(1 - 2 * t);
                float azimuth = angleIncrement * i;
                
                float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
                float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
                float z = Mathf.Cos(inclination);
                
                Vector3 pointOnSphere = new Vector3(x, y, z);
                Vector3 pointPosition = planetCenter + pointOnSphere * planetRadius;
                
                // Create node
                GameObject nodeObj = Instantiate(nodePrefab, pointPosition, Quaternion.identity, container);
                nodeObj.name = $"TestNode_{i}";
                
                PlanetaryNode node = nodeObj.GetComponent<PlanetaryNode>();
                if (node == null)
                {
                    node = nodeObj.AddComponent<PlanetaryNode>();
                }
                
                // Set materials
                MeshRenderer renderer = node.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    // Assign default materials
                    if (i % 3 == 0 && lockedMaterial != null)
                    {
                        renderer.sharedMaterial = lockedMaterial;
                    }
                    else if (i % 3 == 1 && availableMaterial != null)
                    {
                        renderer.sharedMaterial = availableMaterial;
                    }
                    else if (i % 3 == 2 && ownedMaterial != null)
                    {
                        renderer.sharedMaterial = ownedMaterial;
                    }
                }
                
                // Initialize the node
                NodeState initialState = (NodeState)(i % 3); // Cycle through Locked, Available, Owned
                node.Initialize(pointPosition, planet, initialState);
                
                // Store reference
                testNodes[i] = node;
            }
            
            // Connect neighboring nodes
            ConnectNodes();
        }
        
        private void ConnectNodes()
        {
            // Connect each node to its nearest neighbors
            for (int i = 0; i < testNodes.Length; i++)
            {
                PlanetaryNode node = testNodes[i];
                
                // Connect to the next node in sequence (creates a ring)
                int nextIndex = (i + 1) % testNodes.Length;
                node.ConnectTo(testNodes[nextIndex]);
                
                // Connect to a random node for more interesting connections
                int randomIndex = Random.Range(0, testNodes.Length);
                if (randomIndex != i && randomIndex != nextIndex)
                {
                    node.ConnectTo(testNodes[randomIndex]);
                }
            }
        }
        
        private void Update()
        {
            // Test node state cycling with keyboard
            if (Input.GetKeyDown(KeyCode.Space) && testNodes != null && testNodes.Length > 0)
            {
                // Cycle the state of a random node
                int randomIndex = Random.Range(0, testNodes.Length);
                PlanetaryNode node = testNodes[randomIndex];
                
                NodeState newState = (NodeState)(((int)node.State + 1) % 3);
                node.UpdateState(newState);
                
                Debug.Log($"Changed node {node.name} state to {newState}");
            }
        }
    }
} 