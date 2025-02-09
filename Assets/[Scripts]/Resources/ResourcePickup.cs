using UnityEngine;
using Planetarium;

namespace Planetarium
{
    [RequireComponent(typeof(SphereCollider))]
    public class ResourcePickup : CoreBehaviour
    {
        [Header("Resource Settings")]
        public ResourceType resourceType;
        public int amount = 1;

        [Header("Drop Settings")]
        [SerializeField] private float dropSpeed = 10f;
        [SerializeField] private float pickupRadius = 1f;
        [SerializeField] private LayerMask planetLayer;
        [SerializeField] private LayerMask playerLayer;
        
        [Header("Pickup Settings")]
        [SerializeField] private float pickupRange = 5f;
        [SerializeField] private LayerMask raycastLayers;

        [Header("Despawn Settings")]
        [SerializeField] private float lifeTime = 30f;
        [SerializeField] private float warningStartTime = 5f;

        private PlanetBase targetPlanet;
        private SphereCollider sphereCollider;
        private ResourceManager resourceManager;
        private ResourceInventory resourceInventory;
        private Camera mainCamera;
        private bool isInitialized;
        private bool isCollected;
        private bool isLocked;
        private Vector3 surfaceNormal;
        private float currentLifeTime;

        public bool IsCollectible => !isCollected;

        private void Awake()
        {
            sphereCollider = GetComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = pickupRadius;
            mainCamera = Camera.main;
        }

        private void Start()
        {
            // Find the Scene component in the hierarchy
            var scene = FindObjectOfType<Scene>();
            if (scene != null && !isInitialized)
            {
                Initialize(scene.Context, scene.Context.ResourceManager);
            }
        }

        public void Initialize(SceneContext context, ResourceManager manager)
        {
            if (isInitialized) return;
            
            targetPlanet = context.CurrentPlanet;
            resourceManager = manager;
            resourceInventory = context.ResourceInventory;
            currentLifeTime = 0f;
            isCollected = false;
            isLocked = false;
            isInitialized = true;
        }

        public void ResetState()
        {
            // Reset all state variables
            currentLifeTime = 0f;
            isCollected = false;
            isLocked = false;
            surfaceNormal = Vector3.up;
            
            // Reset transform
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            
            // Ensure collider is enabled
            if (sphereCollider != null)
            {
                sphereCollider.enabled = true;
            }
        }

        private void OnDisable()
        {
            // Clean up when object is disabled (returned to pool)
            StopAllCoroutines();
            isCollected = true; // Prevent any pending collections
            
            // Ensure we're not processing any physics
            if (sphereCollider != null)
            {
                sphereCollider.enabled = false;
            }

            // Ensure the object is truly inactive
            if (gameObject != null && gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            // Initialize physics when enabled (taken from pool)
            if (sphereCollider != null)
            {
                sphereCollider.enabled = true;
            }
            
            // Reset state when enabled
            ResetState();
        }

        private void Update()
        {
            if (!isInitialized || isCollected) return;

            currentLifeTime += Time.deltaTime;
            if (currentLifeTime >= lifeTime)
            {
                Collect();
                return;
            }

            if (!isLocked)
            {
                // Drop towards planet
                Vector3 toPlanet = (targetPlanet.transform.position - transform.position).normalized;
                transform.position += toPlanet * dropSpeed * Time.deltaTime;

                // Check for planet surface
                RaycastHit hit;
                if (Physics.Raycast(transform.position, toPlanet, out hit, 1f, planetLayer))
                {
                    surfaceNormal = hit.normal;
                    isLocked = true;
                }
            }
            else
            {
                // Stay locked to planet surface
                Vector3 toPlanet = (targetPlanet.transform.position - transform.position).normalized;
                RaycastHit hit;
                if (Physics.Raycast(transform.position, toPlanet, out hit, 2f, planetLayer))
                {
                    transform.position = hit.point + hit.normal * 0.5f;
                    surfaceNormal = hit.normal;
                }
                transform.rotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
            }

            // Check for touch/click input
            if (Input.GetMouseButtonDown(0) && mainCamera != null)
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                
                // Use the same layer as the sphere collider
                int resourceLayer = gameObject.layer;
                if (Physics.Raycast(ray, out hit))
                {
                    // Check if we hit this resource
                    if (hit.collider.gameObject == gameObject)
                {
                        Debug.Log("Hit resource: " + gameObject.name);
                    Collect();
                }
            }
        }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Left empty intentionally - collection now handled through clicking/tapping
        }

        private void Collect()
        {
            if (isCollected) return;
            isCollected = true;

            // Convert world position to screen position for the popup
            Vector2 screenPos = mainCamera.WorldToScreenPoint(transform.position);
            resourceInventory?.AddResource(resourceType, amount, screenPos);
            
            // Return to pool instead of destroying
            if (resourceManager != null)
            {
                resourceManager.ReleaseResource(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
