using UnityEngine;

namespace Planetarium
{
    [RequireComponent(typeof(PlanetaryNode))]
    public class PlanetaryNodeVisual : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private float pulseSpeed = 1f;
        [SerializeField] private float pulseAmount = 0.2f;
        [SerializeField] private float hoverHeight = 0.2f; // How much to raise the node when selected
        
        [Header("Node Materials")]
        [SerializeField] private Material lockedMaterial;
        [SerializeField] private Material availableMaterial;
        [SerializeField] private Material ownedMaterial;
        [SerializeField] private Material selectedMaterial;
        
        private PlanetaryNode node;
        private MeshRenderer meshRenderer;
        private Vector3 originalPosition;
        private Vector3 originalScale;
        private bool isSelected = false;
        private float pulseTimer = 0f;
        private PlanetBase planet;
        
        private void Awake()
        {
            node = GetComponent<PlanetaryNode>();
            meshRenderer = GetComponent<MeshRenderer>();
            
            if (meshRenderer == null)
            {
                Debug.LogError("PlanetaryNodeVisual requires a MeshRenderer component");
                return;
            }
            
            originalScale = transform.localScale;
            originalPosition = transform.position;
        }
        
        private void Start()
        {
            // Subscribe to node state changes
            node.OnStateChanged += UpdateVisual;
            UpdateVisual(node.State);
            
            // Find the planet reference
            if (planet == null)
            {
                planet = FindFirstObjectByType<PlanetBase>();
            }
        }
        
        private void Update()
        {
            // If available, pulse the node to make it more noticeable
            if (node.State == NodeState.Available)
            {
                pulseTimer += Time.deltaTime * pulseSpeed;
                float pulse = 1f + (Mathf.Sin(pulseTimer) * pulseAmount);
                
                // Only pulse the scale on the X and Z axes to maintain the disc shape
                transform.localScale = new Vector3(
                    originalScale.x * pulse,
                    originalScale.y,
                    originalScale.z * pulse
                );
            }
            
            // If selected, raise it slightly above the surface
            if (isSelected && planet != null)
            {
                Vector3 dirFromCenter = (transform.position - planet.transform.position).normalized;
                transform.position = originalPosition + dirFromCenter * hoverHeight;
            }
            else
            {
                transform.position = originalPosition;
            }
        }
        
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            
            if (!isSelected)
            {
                // Reset to normal position if not selected
                transform.position = originalPosition;
                UpdateVisual(node.State);
            }
        }
        
        private void UpdateVisual(NodeState state)
        {
            if (meshRenderer == null) return;
            
            switch (state)
            {
                case NodeState.Locked:
                    meshRenderer.material = lockedMaterial;
                    transform.localScale = originalScale;
                    break;
                case NodeState.Available:
                    meshRenderer.material = availableMaterial;
                    // Scale is handled in Update for pulsing
                    break;
                case NodeState.Owned:
                    meshRenderer.material = ownedMaterial;
                    transform.localScale = originalScale;
                    break;
            }
            
            // If selected, override with selection material
            if (isSelected)
            {
                meshRenderer.material = selectedMaterial ?? meshRenderer.material;
            }
        }
        
        private void OnDestroy()
        {
            if (node != null)
            {
                node.OnStateChanged -= UpdateVisual;
            }
        }
    }
} 