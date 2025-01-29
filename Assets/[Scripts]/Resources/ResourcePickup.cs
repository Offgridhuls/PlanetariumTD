using UnityEngine;
using Planetarium;

namespace Planetarium
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class ResourcePickup : CoreBehaviour
    {
        [Header("Resource Settings")]
        public ResourceType resourceType;
        public int amount = 1;

        [Header("Physics Settings")]
        [SerializeField] private float initialUpwardForce = 5f;
        [SerializeField] private float initialRandomForce = 2f;
        [SerializeField] private float dragAmount = 1f;
        [SerializeField] private float floatHeight = 0.5f;
        [SerializeField] private LayerMask planetLayer;
        [SerializeField] private LayerMask playerLayer;
        
        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 45f;
        [SerializeField] private float wobbleAmount = 5f;
        [SerializeField] private float wobbleSpeed = 2f;

        [Header("Despawn Visual Settings")]
        [SerializeField] private Color despawnWarningColor = new Color(1f, 0.5f, 0f, 1f); // Orange warning color
        [SerializeField] private float warningStartTime = 5f; // Start warning 5 seconds before despawn
        [SerializeField] private float blinkSpeed = 2f; // How fast to blink when near despawn
        [SerializeField] private float fadeStartTime = 2f; // Start fading 2 seconds before despawn
        [SerializeField] private ParticleSystem collectEffect;

        private PlanetBase targetPlanet;
        private Rigidbody rb;
        private SphereCollider sphereCollider;
        private SpriteRenderer spriteRenderer;
        private ResourceManager resourceManager;
        private ResourceInventory resourceInventory;
        private bool isInitialized;
        private bool isCollected;
        private bool isFloating;
        private float wobbleOffset;
        private static readonly RaycastHit[] raycastHits = new RaycastHit[1];
        private float nextRaycastTime;
        private const float RAYCAST_INTERVAL = 0.1f;
        private Vector3 lastGravityDir;
        private float currentRotation;
        private float lifeTime;
        private const float MAX_LIFETIME = 30f; // Maximum time before auto-collecting
        private Color originalColor;

        public bool IsCollectible => !isCollected;

        private void Awake()
        {
            // Get and cache components
            rb = GetComponent<Rigidbody>();
            sphereCollider = GetComponent<SphereCollider>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            // Configure Rigidbody
            rb.useGravity = false;
            rb.linearDamping = dragAmount;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            
            // Configure Collider
            sphereCollider.isTrigger = true;
        }

        public void Initialize(SceneContext context, ResourceManager manager)
        {
            if (isInitialized) return;
            
            targetPlanet = context.CurrentPlanet;
            resourceManager = manager;
            resourceInventory = context.ResourceInventory;
            lifeTime = 0f;
            isCollected = false;
            isFloating = false;

            // Update collider radius
            sphereCollider.radius = resourceType.pickupRadius;

            if (spriteRenderer && resourceType)
            {
                spriteRenderer.color = resourceType.resourceColor;
                originalColor = resourceType.resourceColor;
            }

            // Add initial forces
            Vector3 randomDirection = Random.insideUnitSphere;
            randomDirection.y = Mathf.Abs(randomDirection.y);
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce((Vector3.up * initialUpwardForce + randomDirection * initialRandomForce), ForceMode.Impulse);

            // Initialize rotation values
            wobbleOffset = Random.value * Mathf.PI * 2f;
            currentRotation = Random.value * 360f;

            isInitialized = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected || !isInitialized) return;

            // Check if the colliding object is on the player layer
            if (((1 << other.gameObject.layer) & playerLayer) != 0)
            {
                TryCollect();
            }
        }

        private void FixedUpdate()
        {
            if (!isInitialized || isCollected || !targetPlanet) return;

            // Update lifetime and check for auto-collection
            lifeTime += Time.fixedDeltaTime;
            if (lifeTime >= MAX_LIFETIME)
            {
                TryCollect();
                return;
            }

            // Update visuals based on remaining lifetime
            UpdateDespawnVisuals();

            Vector3 directionToPlanet = (targetPlanet.transform.position - transform.position);
            float distanceToCenter = directionToPlanet.magnitude;
            
            // Prevent division by zero and NaN
            if (distanceToCenter < 0.001f)
            {
                directionToPlanet = Vector3.up;
                distanceToCenter = 0.001f;
            }
            
            directionToPlanet = directionToPlanet / distanceToCenter;

            // Only raycast periodically
            if (Time.time >= nextRaycastTime)
            {
                int hitCount = Physics.RaycastNonAlloc(transform.position, directionToPlanet, raycastHits, 100f, planetLayer);
                if (hitCount > 0)
                {
                    float distanceToSurface = raycastHits[0].distance;
                    isFloating = distanceToSurface <= floatHeight;
                    lastGravityDir = directionToPlanet;
                }
                else
                {
                    isFloating = false;
                    lastGravityDir = directionToPlanet;
                }

                nextRaycastTime = Time.time + RAYCAST_INTERVAL;
            }

            // Ensure lastGravityDir is valid
            if (lastGravityDir == Vector3.zero)
            {
                lastGravityDir = directionToPlanet;
            }

            // Apply forces
            if (isFloating && raycastHits[0].distance > 0)
            {
                float upwardForce = Mathf.Clamp((floatHeight - raycastHits[0].distance) * resourceType.gravitationSpeed, -100f, 100f);
                Vector3 force = -lastGravityDir * upwardForce;
                
                // Prevent NaN forces
                if (!float.IsNaN(force.x) && !float.IsNaN(force.y) && !float.IsNaN(force.z))
                {
                    rb.AddForce(force, ForceMode.Force);
                }
                
                currentRotation += rotationSpeed * Time.fixedDeltaTime;
                float wobble = Mathf.Sin((Time.time + wobbleOffset) * wobbleSpeed) * wobbleAmount;
                
                // Ensure we have a valid up vector for rotation
                Vector3 upVector = -lastGravityDir;
                if (upVector.magnitude < 0.001f)
                {
                    upVector = Vector3.up;
                }
                
                try
                {
                    // Create rotation safely
                    Quaternion baseRotation = Quaternion.FromToRotation(Vector3.up, upVector);
                    Quaternion wobbleRotation = Quaternion.Euler(wobble, currentRotation, 0);
                    transform.rotation = baseRotation * wobbleRotation;
                }
                catch
                {
                    // Fallback rotation if something goes wrong
                    transform.rotation = Quaternion.identity;
                }
            }
            else
            {
                float gravitationalForce = resourceType.gravitationSpeed * resourceManager.GetGravitationMultiplier();
                Vector3 force = directionToPlanet * gravitationalForce;
                
                // Prevent NaN forces
                if (!float.IsNaN(force.x) && !float.IsNaN(force.y) && !float.IsNaN(force.z))
                {
                    rb.AddForce(force, ForceMode.Force);
                }
                
                // Safe rotation
                if (directionToPlanet.magnitude >= 0.001f)
                {
                    transform.rotation = Quaternion.FromToRotation(Vector3.up, -directionToPlanet);
                }
            }
        }

        private void UpdateDespawnVisuals()
        {
            if (!spriteRenderer) return;

            float remainingTime = MAX_LIFETIME - lifeTime;
            
            if (remainingTime <= fadeStartTime)
            {
                // Final fade out
                float alpha = Mathf.Clamp01(remainingTime / fadeStartTime);
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, alpha);
            }
            else if (remainingTime <= warningStartTime)
            {
                // Warning blink effect
                float blinkValue = Mathf.PingPong(Time.time * blinkSpeed, 1f);
                spriteRenderer.color = Color.Lerp(originalColor, despawnWarningColor, blinkValue);
            }
            else
            {
                // Normal color
                spriteRenderer.color = originalColor;
            }
        }

        public bool TryCollect()
        {
            if (isCollected || !isInitialized) return false;
            isCollected = true;

            // Play collection effect if assigned
            if (collectEffect != null)
            {
                var effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
                effect.Play();
                Destroy(effect.gameObject, effect.main.duration);
            }

            // Add to inventory if available
            if (resourceInventory != null)
            {
                resourceInventory.AddResource(resourceType, amount);
            }

            // Notify resource manager
            resourceManager.CollectResource(resourceType, amount);
            resourceManager.ReleaseResource(this);

            return true;
        }
    }
}
