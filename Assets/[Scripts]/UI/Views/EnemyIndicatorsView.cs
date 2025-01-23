using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Planetarium.UI
{
    public class EnemyIndicatorsView : UIView
    {
        [System.Serializable]
        private class IndicatorPool
        {
            public GameObject indicatorPrefab;
            public int poolSize = 20;
            public List<GameObject> activeIndicators = new List<GameObject>();
            public Queue<GameObject> inactiveIndicators = new Queue<GameObject>();
        }

        [Header("Indicator Settings")]
        [SerializeField] private IndicatorPool normalEnemyPool;
        [SerializeField] private IndicatorPool bossEnemyPool;
        [SerializeField] private float edgeBuffer = 50f;
        [SerializeField] private float updateInterval = 0.1f;
        
        [Header("Visibility")]
        [SerializeField] private float fadeStartDistance = 10f;
        [SerializeField] private float fadeEndDistance = 5f;

        private Camera mainCamera;
        private List<EnemyBase> trackedEnemies = new List<EnemyBase>();
        private float nextUpdateTime;
        private Canvas parentCanvas;
        private Vector2 screenBounds;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            mainCamera = Camera.main;
            parentCanvas = GetComponentInParent<Canvas>();
            
            InitializePool(normalEnemyPool);
            InitializePool(bossEnemyPool);
            
            UpdateScreenBounds();
        }

        protected override void OnDeinitialize()
        {
            base.OnDeinitialize();
            
            ClearPool(normalEnemyPool);
            ClearPool(bossEnemyPool);
            trackedEnemies.Clear();
        }

        protected override void OnTick()
        {
            base.OnTick();

            if (Time.time >= nextUpdateTime)
            {
                UpdateIndicators();
                nextUpdateTime = Time.time + updateInterval;
            }
        }

        private void InitializePool(IndicatorPool pool)
        {
            for (int i = 0; i < pool.poolSize; i++)
            {
                GameObject indicator = Instantiate(pool.indicatorPrefab, transform);
                indicator.SetActive(false);
                pool.inactiveIndicators.Enqueue(indicator);
            }
        }

        public void RegisterEnemy(EnemyBase enemy)
        {
            if (!trackedEnemies.Contains(enemy))
            {
                trackedEnemies.Add(enemy);
                //PlaySound(Context.GetAudioSetup("EnemyDetected"));
            }
        }

        public void UnregisterEnemy(EnemyBase enemy)
        {
            trackedEnemies.Remove(enemy);
        }

        private void UpdateIndicators()
        {
            ClearPool(normalEnemyPool);
            ClearPool(bossEnemyPool);

            UpdateScreenBounds();

            foreach (var enemy in trackedEnemies)
            {
                if (enemy == null) continue;

                Vector3 screenPoint = mainCamera.WorldToScreenPoint(enemy.transform.position);
                bool isOffscreen = IsOffscreen(screenPoint);

                if (isOffscreen)
                {
                    IndicatorPool pool = normalEnemyPool;
                    
                    GameObject indicator = GetIndicator(pool);
                    if (indicator == null) continue;

                    Vector2 indicatorPosition = CalculateIndicatorPosition(screenPoint);
                    indicator.transform.position = indicatorPosition;

                    float angle = CalculateIndicatorAngle(indicatorPosition, screenPoint);
                    indicator.transform.rotation = Quaternion.Euler(0, 0, angle);

                    UpdateIndicatorVisibility(indicator, enemy);
                }
            }

            trackedEnemies.RemoveAll(e => e == null);
        }

        private void UpdateScreenBounds()
        {
            screenBounds = new Vector2(Screen.width - edgeBuffer, Screen.height - edgeBuffer);
        }

        private bool IsOffscreen(Vector3 screenPoint)
        {
            return screenPoint.x < edgeBuffer || screenPoint.x > screenBounds.x ||
                   screenPoint.y < edgeBuffer || screenPoint.y > screenBounds.y ||
                   screenPoint.z < 0;
        }

        private Vector2 CalculateIndicatorPosition(Vector3 screenPoint)
        {
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 screenPos = new Vector2(screenPoint.x, screenPoint.y);
            Vector2 direction = (screenPos - screenCenter).normalized;
            
            float angle = Mathf.Atan2(direction.y, direction.x);
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            
            float intersectX = cos > 0 ? screenBounds.x : edgeBuffer;
            float intersectY = sin > 0 ? screenBounds.y : edgeBuffer;
            
            float x = Mathf.Abs(intersectX / cos);
            float y = Mathf.Abs(intersectY / sin);
            
            return screenCenter + direction * Mathf.Min(x, y);
        }

        private float CalculateIndicatorAngle(Vector2 indicatorPos, Vector3 targetScreenPos)
        {
            Vector2 direction = new Vector2(targetScreenPos.x, targetScreenPos.y) - indicatorPos;
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        private void UpdateIndicatorVisibility(GameObject indicator, EnemyBase enemy)
        {
            float distance = Vector3.Distance(mainCamera.transform.position, enemy.transform.position);
            float alpha = Mathf.Clamp01((distance - fadeEndDistance) / (fadeStartDistance - fadeEndDistance));
            
            CanvasGroup canvasGroup = indicator.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }
        }

        private GameObject GetIndicator(IndicatorPool pool)
        {
            GameObject indicator;
            if (pool.inactiveIndicators.Count > 0)
            {
                indicator = pool.inactiveIndicators.Dequeue();
            }
            else if (pool.activeIndicators.Count > 0)
            {
                indicator = pool.activeIndicators[0];
                pool.activeIndicators.RemoveAt(0);
            }
            else
            {
                return null;
            }

            indicator.SetActive(true);
            pool.activeIndicators.Add(indicator);
            return indicator;
        }

        private void ClearPool(IndicatorPool pool)
        {
            foreach (var indicator in pool.activeIndicators)
            {
                indicator.SetActive(false);
                pool.inactiveIndicators.Enqueue(indicator);
            }
            pool.activeIndicators.Clear();
        }
    }
}
