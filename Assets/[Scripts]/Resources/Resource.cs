using UnityEngine;

namespace Planetarium
{
    public class Resource : MonoBehaviour
    {
        [SerializeField]
        private ResourceType resourceType;
        [SerializeField]
        private int amount = 1;
        [SerializeField]
        private bool autoCollect = true;
        [SerializeField]
        private float collectDelay = 0f;
        [SerializeField]
        private ParticleSystem collectEffect;

        private bool isCollectible = true;
        private float spawnTime;

        public bool IsCollectible => isCollectible && (collectDelay <= 0f || Time.time - spawnTime >= collectDelay);

        private void Start()
        {
            spawnTime = Time.time;
        }

        public bool TryCollect(out ResourceType type, out int collectedAmount)
        {
            type = resourceType;
            collectedAmount = amount;

            if (!IsCollectible)
            {
                return false;
            }

            // Play collection effect if assigned
            if (collectEffect != null)
            {
                var effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
                effect.Play();
                Destroy(effect.gameObject, effect.main.duration);
            }

            // Destroy the resource object
            Destroy(gameObject);
            return true;
        }

        private void OnValidate()
        {
            if (amount < 1) amount = 1;
            if (collectDelay < 0f) collectDelay = 0f;
        }
    }
}
