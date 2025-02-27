using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Planetarium
{
    public class PlanetaryExpansionService : SceneService
    {
        [Header("Node Generation")]
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private int gridResolution = 32;  // Higher values = more nodes
        [SerializeField] private float nodeRadius = 5f;
        [SerializeField] private float connectionMaxDistance = 15f;
        
        [Header("Expansion Settings")]
        [SerializeField] private int expansionCost = 10;
        [SerializeField] private bool allowFreeExpansion = false;  // If true, can claim any available node, not just adjacent
        [SerializeField] private float nodeSelectionDistance = 20f;  // Max distance to select a node
        
        [Header("Visuals")]
        [SerializeField] private Material lockedNodeMaterial;
        [SerializeField] private Material availableNodeMaterial;
        [SerializeField] private Material ownedNodeMaterial;
        [SerializeField] private LineRenderer connectionPrefab;
        
        public event Action<PlanetaryNode> OnNodeClaimed;
        public event Action<PlanetaryNode> OnNodeCreated;
        public event Action<int> OnExpansionCostChanged;
        
        private PlanetBase planet;
        private List<PlanetaryNode> allNodes = new List<PlanetaryNode>();
        private PlanetaryNode startNode;
        private PlanetaryNode selectedNode;
        private GameStateManager gameState;
        private Camera mainCamera;
        private Transform nodesContainer;
        
        // Properties
        public int CurrentExpansionCost => expansionCost;
        public int OwnedNodeCount => allNodes.Count(n => n.State == NodeState.Owned);
        public int AvailableNodeCount => allNodes.Count(n => n.State == NodeState.Available);
        public bool CanExpand => allowFreeExpansion ? AvailableNodeCount > 0 : true;
        
        protected override void OnInitialize()
        {
            gameState = Context.GameState;
            planet = FindFirstObjectByType<PlanetBase>();
            mainCamera = Camera.main;
            
            // Create a container for all nodes
            nodesContainer = new GameObject("PlanetaryNodes").transform;
            
            if (planet != null)
            {
                GenerateNodeGrid();
            }
            else
            {
                Debug.LogError("PlanetaryExpansionService: No planet found in the scene!");
            }
        }
        
        protected override void OnTick()
        {
            if (!IsActive || planet == null) return;
            
            HandleNodeSelection();
        }
        
        private void HandleNodeSelection()
        {
            if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    PlanetaryNode node = hit.collider.GetComponent<PlanetaryNode>();
                    if (node != null)
                    {
                        SelectNode(node);
                        
                        // Auto-claim if available
                        if (node.State == NodeState.Available)
                        {
                            TryClaimNode(node);
                        }
                    }
                }
            }
        }
        
        private void GenerateNodeGrid()
        {
            // Clear existing nodes
            foreach (var node in allNodes)
            {
                if (node != null)
                {
                    Destroy(node.gameObject);
                }
            }
            allNodes.Clear();
            
            // Generate an approximately evenly distributed set of points on the sphere
            // using the Fibonacci sphere algorithm
            float planetRadius = planet.GetPlanetRadius();
            Vector3 planetCenter = planet.transform.position;
            
            // Use the golden ratio for optimal point distribution
            float goldenRatio = (1 + Mathf.Sqrt(5)) / 2;
            float angleIncrement = 2 * Mathf.PI * goldenRatio;
            
            for (int i = 0; i < gridResolution; i++)
            {
                // Calculate the position on the unit sphere using the Fibonacci algorithm
                float t = (float)i / gridResolution;
                float inclination = Mathf.Acos(1 - 2 * t);
                float azimuth = angleIncrement * i;
                
                float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
                float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
                float z = Mathf.Cos(inclination);
                
                Vector3 pointOnSphere = new Vector3(x, y, z);
                Vector3 pointPosition = planetCenter + pointOnSphere * planetRadius;
                
                // Create a node at this position
                CreateNode(pointPosition);
            }
            
            // Connect neighboring nodes
            ConnectNodes();
            
            // Select a start node
            if (allNodes.Count > 0)
            {
                // Choose a random node as the starting point
                int startIndex = UnityEngine.Random.Range(0, allNodes.Count);
                startNode = allNodes[startIndex];
                
                // Make the start node owned and adjacent nodes available
                startNode.UpdateState(NodeState.Owned);
                foreach (var node in startNode.ConnectedNodes)
                {
                    node.UpdateState(NodeState.Available);
                }
                
                OnNodeClaimed?.Invoke(startNode);
            }
            
            Debug.Log($"PlanetaryExpansionService: Generated {allNodes.Count} nodes on the planet surface.");
        }
        
        private PlanetaryNode CreateNode(Vector3 position)
        {
            GameObject nodeObj = Instantiate(nodePrefab, position, Quaternion.identity, nodesContainer);
            nodeObj.name = $"Node_{allNodes.Count}";
            
            PlanetaryNode node = nodeObj.GetComponent<PlanetaryNode>();
            if (node == null)
            {
                node = nodeObj.AddComponent<PlanetaryNode>();
            }
            
            // Set materials if they're not already set
            if (node.GetComponent<MeshRenderer>().sharedMaterial == null)
            {
                // Assign materials based on state
                if (lockedNodeMaterial != null) node.GetComponent<MeshRenderer>().sharedMaterial = lockedNodeMaterial;
            }
            
            // Initialize the node
            node.Initialize(position, planet);
            allNodes.Add(node);
            
            // Notify listeners that a new node was created
            OnNodeCreated?.Invoke(node);
            
            return node;
        }
        
        private void ConnectNodes()
        {
            // Connect each node to its neighbors within connectionMaxDistance
            foreach (var node in allNodes)
            {
                foreach (var otherNode in allNodes)
                {
                    if (node == otherNode)
                        continue;
                    
                    float distance = node.GetDistanceTo(otherNode);
                    if (distance <= connectionMaxDistance)
                    {
                        node.ConnectTo(otherNode);
                    }
                }
            }
            
            // Ensure all nodes are connected - connect isolated nodes to their nearest neighbor
            List<PlanetaryNode> isolatedNodes = allNodes.Where(n => n.ConnectedNodes.Count == 0).ToList();
            foreach (var isolatedNode in isolatedNodes)
            {
                PlanetaryNode nearestNode = FindNearestNode(isolatedNode, allNodes.Where(n => n != isolatedNode).ToList());
                if (nearestNode != null)
                {
                    isolatedNode.ConnectTo(nearestNode);
                }
            }
            
            // Ensure the graph is fully connected by connecting separate clusters
            EnsureFullyConnectedGraph();
        }
        
        private void EnsureFullyConnectedGraph()
        {
            // Use a simple flood fill to identify disconnected clusters
            List<List<PlanetaryNode>> clusters = new List<List<PlanetaryNode>>();
            HashSet<PlanetaryNode> visitedNodes = new HashSet<PlanetaryNode>();
            
            foreach (var node in allNodes)
            {
                if (!visitedNodes.Contains(node))
                {
                    // Start a new cluster
                    List<PlanetaryNode> cluster = new List<PlanetaryNode>();
                    Queue<PlanetaryNode> queue = new Queue<PlanetaryNode>();
                    
                    queue.Enqueue(node);
                    visitedNodes.Add(node);
                    
                    while (queue.Count > 0)
                    {
                        PlanetaryNode current = queue.Dequeue();
                        cluster.Add(current);
                        
                        foreach (var connected in current.ConnectedNodes)
                        {
                            if (!visitedNodes.Contains(connected))
                            {
                                queue.Enqueue(connected);
                                visitedNodes.Add(connected);
                            }
                        }
                    }
                    
                    clusters.Add(cluster);
                }
            }
            
            // If we have more than one cluster, connect them
            if (clusters.Count > 1)
            {
                for (int i = 0; i < clusters.Count - 1; i++)
                {
                    // Find the closest pair of nodes between clusters i and i+1
                    PlanetaryNode closestInCluster1 = null;
                    PlanetaryNode closestInCluster2 = null;
                    float minDistance = float.MaxValue;
                    
                    foreach (var node1 in clusters[i])
                    {
                        foreach (var node2 in clusters[i + 1])
                        {
                            float distance = node1.GetDistanceTo(node2);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                closestInCluster1 = node1;
                                closestInCluster2 = node2;
                            }
                        }
                    }
                    
                    // Connect the closest nodes
                    if (closestInCluster1 != null && closestInCluster2 != null)
                    {
                        closestInCluster1.ConnectTo(closestInCluster2);
                    }
                }
            }
        }
        
        private PlanetaryNode FindNearestNode(PlanetaryNode sourceNode, List<PlanetaryNode> candidateNodes)
        {
            if (candidateNodes.Count == 0)
                return null;
                
            PlanetaryNode nearest = null;
            float minDistance = float.MaxValue;
            
            foreach (var candidate in candidateNodes)
            {
                float distance = sourceNode.GetDistanceTo(candidate);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = candidate;
                }
            }
            
            return nearest;
        }
        
        public void SelectNode(PlanetaryNode node)
        {
            if (node == null)
                return;
                
            // Deselect previous node if any
            if (selectedNode != null)
            {
                PlanetaryNodeVisual visual = selectedNode.GetComponent<PlanetaryNodeVisual>();
                if (visual != null)
                {
                    visual.SetSelected(false);
                }
            }
            
            selectedNode = node;
            
            // Visualize selection
            PlanetaryNodeVisual nodeVisual = selectedNode.GetComponent<PlanetaryNodeVisual>();
            if (nodeVisual != null)
            {
                nodeVisual.SetSelected(true);
            }
            
            Debug.Log($"Selected node: {node.name}, State: {node.State}");
        }
        
        public bool TryClaimNode(PlanetaryNode node)
        {
            if (node == null || node.State != NodeState.Available)
                return false;
                
            // Check if we can claim this node
            bool canClaim = false;
            
            if (allowFreeExpansion)
            {
                canClaim = true;
            }
            else
            {
                // Check if the node is adjacent to an owned node
                foreach (var connectedNode in node.ConnectedNodes)
                {
                    if (connectedNode.State == NodeState.Owned)
                    {
                        canClaim = true;
                        break;
                    }
                }
            }
            
            if (!canClaim)
            {
                Debug.Log("Cannot claim this node - it's not adjacent to any owned nodes.");
                return false;
            }
            
            /*// Check if we have enough resources
            if (gameState != null && !gameState.TrySpendResource(ResourceType.Energy, expansionCost))
            {
                Debug.Log($"Not enough energy to claim node. Required: {expansionCost}");
                return false;
            }*/
            
            // Claim the node
            node.Claim();
            
            // Notify listeners
            OnNodeClaimed?.Invoke(node);
            
            Debug.Log($"Node claimed: {node.name}");
            return true;
        }
        
        public void SetExpansionCost(int newCost)
        {
            expansionCost = newCost;
            OnExpansionCostChanged?.Invoke(expansionCost);
        }
        
        public List<PlanetaryNode> GetOwnedNodes()
        {
            return allNodes.Where(n => n.State == NodeState.Owned).ToList();
        }
        
        public List<PlanetaryNode> GetAvailableNodes()
        {
            return allNodes.Where(n => n.State == NodeState.Available).ToList();
        }
        
        public List<PlanetaryNode> GetAllNodes()
        {
            return new List<PlanetaryNode>(allNodes);
        }
        
        protected override void OnDeinitialize()
        {
            // Clean up
            if (nodesContainer != null)
            {
                Destroy(nodesContainer.gameObject);
            }
            
            allNodes.Clear();
        }
    }
} 