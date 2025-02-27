using UnityEngine;
using System.Collections.Generic;
using System;
using Planetarium.Stats;

namespace Planetarium
{
    public enum NodeState
    {
        Locked,      // Node is not yet available for acquisition
        Available,   // Node is available to be claimed
        Owned        // Node is already owned by the player
    }

    [RequireComponent(typeof(TaggedComponent))]
    public class PlanetaryNode : MonoBehaviour, ITaggable
    {
        [Header("Node Settings")]
        [SerializeField] private float nodeRadius = 5f;
        [SerializeField] private float nodeHeight = 0.5f; // Height of the node above the surface
        [SerializeField] private Material lockedMaterial;
        [SerializeField] private Material availableMaterial;
        [SerializeField] private Material ownedMaterial;

        [Header("Connections")]
        [SerializeField] private LineRenderer connectionPrefab;
        
        [Header("Tag Settings")]
        [SerializeField] private GameplayTag planetaryNodeTag;
        [SerializeField] private GameplayTag lockedNodeTag;
        [SerializeField] private GameplayTag availableNodeTag;
        [SerializeField] private GameplayTag ownedNodeTag;
        
        public NodeState State { get; private set; } = NodeState.Locked;
        public Vector3 SurfacePosition { get; private set; }
        public List<PlanetaryNode> ConnectedNodes { get; private set; } = new List<PlanetaryNode>();
        
        // Events
        public event Action<NodeState> OnStateChanged;
        
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private List<LineRenderer> activeConnections = new List<LineRenderer>();
        private PlanetBase planet;
        private TaggedComponent taggedComponent;

        // ITaggable implementation
        public void OnTagAdded(GameplayTag tag) { /* Implementation not critical for this class */ }
        public void OnTagRemoved(GameplayTag tag) { /* Implementation not critical for this class */ }

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            
            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
                // Default to a disc-like mesh
                meshFilter.mesh = CreateDiscMesh(1f, 24);
            }
            
            // Ensure we have a collider for interaction
            if (GetComponent<Collider>() == null)
            {
                SphereCollider collider = gameObject.AddComponent<SphereCollider>();
                collider.radius = 0.5f; // Half of the transform scale
            }
            
            // Set up tagged component
            taggedComponent = GetComponent<TaggedComponent>();
            if (taggedComponent == null)
            {
                taggedComponent = gameObject.AddComponent<TaggedComponent>();
            }
            
            // Add the base planetary node tag if specified
            if (planetaryNodeTag != null)
            {
                taggedComponent.AddTag(planetaryNodeTag);
            }
        }

        public void Initialize(Vector3 position, PlanetBase planetRef, NodeState initialState = NodeState.Locked)
        {
            planet = planetRef;
            
            // Calculate the surface position
            if (planet != null)
            {
                SurfacePosition = planet.GetClosestSurfacePoint(position);
                
                // Position slightly above the surface
                Vector3 dirFromCenter = (SurfacePosition - planet.transform.position).normalized;
                transform.position = SurfacePosition + dirFromCenter * nodeHeight;
                
                // Orient the node to face away from the planet center
                transform.up = dirFromCenter;
                
                // Scale the node based on the planet size and node radius
                float planetRadius = planet.GetPlanetRadius();
                float scaleFactor = nodeRadius / planetRadius;
                transform.localScale = new Vector3(scaleFactor, 0.1f, scaleFactor) * planetRadius;
            }
            else
            {
                SurfacePosition = position;
                transform.position = position;
            }
            
            UpdateState(initialState);
        }

        public void UpdateState(NodeState newState)
        {
            if (State == newState)
                return;
            
            NodeState previousState = State;
            State = newState;
            
            // Update visual appearance based on state
            switch (State)
            {
                case NodeState.Locked:
                    meshRenderer.material = lockedMaterial;
                    break;
                case NodeState.Available:
                    meshRenderer.material = availableMaterial;
                    break;
                case NodeState.Owned:
                    meshRenderer.material = ownedMaterial;
                    break;
            }
            
            // Update tags based on state
            if (taggedComponent != null)
            {
                // Remove previous state tag
                switch (previousState)
                {
                    case NodeState.Locked:
                        if (lockedNodeTag != null) taggedComponent.RemoveTag(lockedNodeTag);
                        break;
                    case NodeState.Available:
                        if (availableNodeTag != null) taggedComponent.RemoveTag(availableNodeTag);
                        break;
                    case NodeState.Owned:
                        if (ownedNodeTag != null) taggedComponent.RemoveTag(ownedNodeTag);
                        break;
                }
                
                // Add new state tag
                switch (State)
                {
                    case NodeState.Locked:
                        if (lockedNodeTag != null) taggedComponent.AddTag(lockedNodeTag);
                        break;
                    case NodeState.Available:
                        if (availableNodeTag != null) taggedComponent.AddTag(availableNodeTag);
                        break;
                    case NodeState.Owned:
                        if (ownedNodeTag != null) taggedComponent.AddTag(ownedNodeTag);
                        break;
                }
            }
            
            // Notify listeners of state change
            OnStateChanged?.Invoke(State);
        }

        public void ConnectTo(PlanetaryNode otherNode)
        {
            if (otherNode == null || ConnectedNodes.Contains(otherNode))
                return;

            ConnectedNodes.Add(otherNode);
            
            // Add reciprocal connection
            if (!otherNode.ConnectedNodes.Contains(this))
            {
                otherNode.ConnectedNodes.Add(this);
            }
            
            // Visual connection
            if (connectionPrefab != null)
            {
                LineRenderer connection = Instantiate(connectionPrefab);
                connection.positionCount = 2;
                
                // Position the connection slightly above the surface to avoid z-fighting
                Vector3 dirFromCenter1 = (SurfacePosition - planet.transform.position).normalized;
                Vector3 dirFromCenter2 = (otherNode.SurfacePosition - planet.transform.position).normalized;
                
                Vector3 pos1 = SurfacePosition + dirFromCenter1 * (nodeHeight * 0.5f);
                Vector3 pos2 = otherNode.SurfacePosition + dirFromCenter2 * (nodeHeight * 0.5f);
                
                connection.SetPosition(0, pos1);
                connection.SetPosition(1, pos2);
                activeConnections.Add(connection);
            }
        }

        public void Claim()
        {
            if (State != NodeState.Available)
                return;
                
            UpdateState(NodeState.Owned);
            
            // Make adjacent nodes available
            foreach (var node in ConnectedNodes)
            {
                if (node.State == NodeState.Locked)
                {
                    node.UpdateState(NodeState.Available);
                }
            }
        }

        public bool IsAdjacent(PlanetaryNode otherNode)
        {
            return ConnectedNodes.Contains(otherNode);
        }

        public float GetDistanceTo(PlanetaryNode otherNode)
        {
            // This is surface distance, not direct distance
            return Vector3.Distance(SurfacePosition, otherNode.SurfacePosition);
        }
        
        private Mesh CreateDiscMesh(float radius, int segments)
        {
            Mesh mesh = new Mesh();
            
            Vector3[] vertices = new Vector3[segments + 1];
            int[] triangles = new int[segments * 3];
            Vector2[] uv = new Vector2[segments + 1];
            
            // Center vertex
            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);
            
            // Outer vertices
            float angleStep = 2f * Mathf.PI / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                
                vertices[i + 1] = new Vector3(x, 0, z);
                
                // UV coordinates (map to 0-1 range)
                uv[i + 1] = new Vector2(x / (2f * radius) + 0.5f, z / (2f * radius) + 0.5f);
                
                // Create triangles
                triangles[i * 3] = 0; // Center
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % segments + 1;
            }
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            
            return mesh;
        }

        private void OnDestroy()
        {
            // Clean up connection visuals
            foreach (var connection in activeConnections)
            {
                if (connection != null)
                {
                    Destroy(connection.gameObject);
                }
            }
            activeConnections.Clear();
        }
    }
} 