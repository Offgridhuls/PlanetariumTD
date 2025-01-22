using UnityEngine;

public abstract class ProjectileBase : MonoBehaviour
{
    [Header("Base Projectile Settings")]
    [SerializeField] protected float projectileSpeed = 20f;
    [SerializeField] protected float maxLifetime = 3f;
    [SerializeField] protected LayerMask targetLayers;
    [SerializeField] protected float effectLifetime = 2f;

    [Header("Sound Effects")]
    [SerializeField] protected AudioClip launchSound;
    [SerializeField] protected AudioClip hitSound;
    [SerializeField] protected float soundVolume = 0.5f;

    protected Vector3 targetPosition;
    protected GameObject targetEnemy;
    protected int damage;
    protected bool isInitialized;
    protected float aliveTime;
    protected AudioSource audioSource;

    protected virtual void Awake()
    {
        // Set up audio source if we have sound effects
        if (launchSound != null || hitSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.maxDistance = 20f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.volume = soundVolume;
        }
    }

    public virtual void Initialize(int damage, Vector3 target, float speed)
    {
        this.damage = damage;
        this.targetPosition = target;
        this.projectileSpeed = speed;
        aliveTime = 0f;

        // Play launch sound if available
        PlayLaunchSound();
    }

    protected virtual void PlayLaunchSound()
    {
        if (audioSource != null && launchSound != null)
        {
            audioSource.PlayOneShot(launchSound, soundVolume);
        }
    }

    protected virtual void PlayHitSound()
    {
        if (audioSource != null && hitSound != null)
        {
            // Create a temporary audio source for the hit sound
            // This ensures the sound plays even after the projectile is destroyed
            GameObject audioObj = new GameObject("HitSound");
            audioObj.transform.position = transform.position;
            AudioSource tempAudio = audioObj.AddComponent<AudioSource>();
            tempAudio.clip = hitSound;
            tempAudio.spatialBlend = 1f;
            tempAudio.maxDistance = 20f;
            tempAudio.rolloffMode = AudioRolloffMode.Linear;
            tempAudio.volume = soundVolume;
            tempAudio.Play();

            // Destroy the audio object after the sound finishes
            Destroy(audioObj, hitSound.length + 0.1f);
        }
    }

    public abstract void ShootProjectile(Vector3 target, GameObject enemy);

    protected virtual void Update()
    {
        if (!isInitialized) return;

        aliveTime += Time.deltaTime;
        if (aliveTime >= maxLifetime)
        {
            OnProjectileHit();
            Destroy(gameObject);
            return;
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!isInitialized) return;

        // Check if we hit something in our target layers
        if (((1 << other.gameObject.layer) & targetLayers) != 0)
        {
            HandleHit(other.gameObject);
        }
    }

    protected virtual void HandleHit(GameObject hitObject)
    {
        var damageable = hitObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            DamageData damageData = new DamageData
            {
                Damage = damage,
                Source = gameObject
            };
            damageable.ProcessDamage(damageData);
        }

        // Play hit sound before destroying
        PlayHitSound();

        OnProjectileHit();
        Destroy(gameObject);
    }

    public virtual void OnProjectileHit()
    {
        // Override in derived classes for specific hit effects
    }

    protected virtual void OnDestroy()
    {
        // Cleanup any remaining effects
        OnProjectileHit();
    }
}
