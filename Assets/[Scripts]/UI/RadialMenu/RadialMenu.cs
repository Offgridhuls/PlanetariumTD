using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Planetarium;

namespace Planetarium.Deployables
{
    public class RadialMenu : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject menuItemPrefab;
        [SerializeField] private RectTransform menuPanel;

        [Header("Layout Settings")]
        [SerializeField] private float menuRadius = 150f;
        [SerializeField] private float minSelectionDistance = 40f;

        [Header("Interaction Settings")]
        [SerializeField] private float holdTimeThreshold = 0.5f;
        [SerializeField] private float moveThreshold = 50f; // Maximum distance the touch can move before canceling
        [SerializeField] private LayerMask planetLayer;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private float scaleInDuration = 0.2f;

        [Header("Deployables")]
        [SerializeField] private DeployableBase[] availableDeployables;

        private List<RadialMenuItem> menuItems = new List<RadialMenuItem>();
        private Camera mainCamera;
        private CanvasGroup canvasGroup;
        private PlanetBase targetPlanet;
        private TurretPlacementService turretService;
        private Planetarium.CameraControlService cameraService;

        private bool isOpen;
        private bool isHolding;
        private float holdTimer;
        private Vector2 openPosition;
        private Vector2 holdStartPosition;
        private RadialMenuItem currentSelection;

        private void Awake()
        {
            mainCamera = Camera.main;
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            turretService = FindObjectOfType<TurretPlacementService>();
            targetPlanet = FindObjectOfType<PlanetBase>();
            cameraService = FindObjectOfType<Planetarium.CameraControlService>();
            
            // Make sure camera controls are not blocked initially
            if (cameraService != null)
                cameraService.BlockInput(false);
                
            // Initialize in closed state
            CloseMenu();
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            Vector2 inputPos = Vector2.zero;
            bool inputBegan = false;
            bool inputEnded = false;
            bool inputHeld = false;

            // Get input based on platform
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                inputPos = touch.position;
                inputBegan = touch.phase == TouchPhase.Began;
                inputEnded = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                inputHeld = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
            }
            else
            {
                inputPos = Input.mousePosition;
                inputBegan = Input.GetMouseButtonDown(0);
                inputEnded = Input.GetMouseButtonUp(0);
                inputHeld = Input.GetMouseButton(0);
            }

            // Handle input states
            if (inputBegan)
            {
                Ray ray = mainCamera.ScreenPointToRay(inputPos);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, planetLayer))
                {
                    PlanetBase hitPlanet = hit.collider.GetComponentInParent<PlanetBase>();
                    if (hitPlanet != null && hitPlanet == targetPlanet)
                    {
                        isHolding = true;
                        holdTimer = 0f;
                        holdStartPosition = inputPos;
                        openPosition = inputPos;
                    }
                }
            }
            else if (inputHeld && isHolding)
            {
                // Check if we've moved too far from the start position
                float moveDistance = Vector2.Distance(inputPos, holdStartPosition);
                if (moveDistance > moveThreshold)
                {
                    isHolding = false;
                    return;
                }

                if (!isOpen)
                {
                    holdTimer += Time.deltaTime;
                    if (holdTimer >= holdTimeThreshold)
                    {
                        if (cameraService != null)
                            cameraService.BlockInput(true);
                        OpenMenu();
                    }
                }
                else
                {
                    UpdateSelection(inputPos);
                }
            }
            else if (inputEnded)
            {
                if (isOpen && currentSelection != null)
                {
                    currentSelection.Select();
                    CloseMenu();
                }

                isHolding = false;
            }
        }

        private void OpenMenu()
        {
            if (isOpen) return;

            isOpen = true;
            menuPanel.position = openPosition;

            // Create menu items
            float angleStep = 360f / availableDeployables.Length;
            for (int i = 0; i < availableDeployables.Length; i++)
            {
                GameObject itemObj = Instantiate(menuItemPrefab, menuPanel);
                RadialMenuItem item = itemObj.GetComponent<RadialMenuItem>();

                // Position item
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector2 position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * menuRadius;
                itemObj.GetComponent<RectTransform>().anchoredPosition = position;

                // Initialize item
                DeployableBase deployable = availableDeployables[i];
                Sprite icon = deployable.M_TurretStats?.GetIcon();
                string label = deployable.M_TurretStats?.GetName() ?? deployable.name;

                item.Initialize(deployable, icon, label);
                item.SetSelectCallback(() => OnItemSelected(item));

                menuItems.Add(item);
            }

            // Show menu with fade and scale animations
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            menuPanel.localScale = Vector3.zero;
            
            // Start animations
            StartCoroutine(AnimateMenuOpen());
        }

        private IEnumerator AnimateMenuOpen()
        {
            float elapsedTime = 0f;
            
            // Animate fade and scale simultaneously
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / fadeInDuration;
                
                // Fade in
                canvasGroup.alpha = normalizedTime;
                
                // Scale with ease out
                float scaleValue = EaseOutBack(normalizedTime);
                menuPanel.localScale = Vector3.one * scaleValue;
                
                yield return null;
            }
            
            // Ensure final values are set
            canvasGroup.alpha = 1f;
            menuPanel.localScale = Vector3.one;
        }

        private void CloseMenu()
        {
            isOpen = false;
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;

            // Clear menu items
            foreach (var item in menuItems)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            menuItems.Clear();
            currentSelection = null;
            
            // Make sure camera controls are unblocked
            if (cameraService != null)
                cameraService.BlockInput(false);
        }

        private void UpdateSelection(Vector2 inputPosition)
        {
            Vector2 center = menuPanel.position;
            Vector2 direction = (inputPosition - center).normalized;
            float distance = Vector2.Distance(inputPosition, center);

            // Only select if far enough from center
            if (distance < minSelectionDistance)
            {
                SetCurrentSelection(null);
                return;
            }

            // Find closest item based on angle
            float inputAngle = Mathf.Atan2(direction.y, direction.x);
            if (inputAngle < 0) inputAngle += 2 * Mathf.PI;

            float angleStep = 2 * Mathf.PI / menuItems.Count;
            int closestIndex = Mathf.RoundToInt(inputAngle / angleStep);
            if (closestIndex >= menuItems.Count) closestIndex = 0;

            SetCurrentSelection(menuItems[closestIndex]);
        }

        private void SetCurrentSelection(RadialMenuItem newSelection)
        {
            if (currentSelection == newSelection) return;

            if (currentSelection != null)
                currentSelection.SetHighlighted(false);

            currentSelection = newSelection;

            if (currentSelection != null)
                currentSelection.SetHighlighted(true);
        }

        private void OnItemSelected(RadialMenuItem item)
        {
            if (turretService != null && item != null)
            {
                DeployableBase prefab = item.GetDeployablePrefab();
                if (prefab != null)
                    turretService.SelectTurret(prefab);
            }
        }
        
        private float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}