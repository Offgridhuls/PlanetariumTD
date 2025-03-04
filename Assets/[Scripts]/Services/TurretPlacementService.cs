using UnityEngine;
using UnityEngine.EventSystems;
using System;
using Planetarium.Deployables;

namespace Planetarium
{
    [RequireComponent(typeof(CursorController))]
    public class TurretPlacementService : SceneService
    {
        [Header("Placement Settings")]
        [SerializeField] private float placementOffset = -1.5f;  // Much deeper below surface
        [SerializeField] private LayerMask planetLayer;
        [SerializeField] private Color validPlacementColor = new Color(0, 1, 0, 0.5f);
        [SerializeField] private Color invalidPlacementColor = new Color(1, 0, 0, 0.5f);
        [SerializeField] private float minimumTurretDistance = 5f; // Minimum distance between turrets

        private DeployableBase selectedTurret;
        private DeployableBase previewTurret;
        private bool hasSelectedTurret;
        private GameStateManager gameState;
        private CursorController cursorController;
        private Camera mainCamera;
        private bool isValidPlacement;

        // Events
        public event Action<DeployableBase> OnTurretSelectionChanged;

        protected override void OnInitialize()
        {
            gameState = Context.GameState;
            cursorController = GetComponent<CursorController>();
            mainCamera = Camera.main;

            // Ensure cursor is always visible
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        protected override void OnTick()
        {
            if (!IsActive)
                return;

            if (hasSelectedTurret && selectedTurret != null)
            {
                UpdatePreview();

                // Check for mouse input
                if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
                {
                    if (isValidPlacement)
                    {
                        PlaceTurret();
                    }
                }
                // Check for touch input
                else if (Input.touchCount > 0)
                {
                    Touch touch = Input.GetTouch(0);
                    if (touch.phase == TouchPhase.Began && isValidPlacement)
                    {
                        PlaceTurret();
                    }
                }

                // Cancel placement with right click or escape
                if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                {
                    CancelTurretPlacement();
                }
            }
        }

        public void SelectTurret(DeployableBase turretPrefab)
        {
            if (turretPrefab == null)
            {
                CancelTurretPlacement();
                return;
            }

            // Check if we can afford the turret but don't deduct yet
            if (gameState.Currency < turretPrefab.M_TurretStats.GetCoinCost())
            {
                // TODO: Show cannot afford message
                return;
            }

            CancelTurretPlacement(); // Clean up any existing preview
            
            selectedTurret = turretPrefab;
            hasSelectedTurret = true;
            
            // Create preview
            previewTurret = Instantiate(turretPrefab);
            SetupPreviewTurret(previewTurret);

            // Notify selection change
            OnTurretSelectionChanged?.Invoke(selectedTurret);

            // Set placement cursor
            if (cursorController != null)
            {
                cursorController.SetPlacementCursor();
            }
        }

        public void CancelTurretPlacement()
        {
            if (hasSelectedTurret)
            {
                // Clean up preview
                if (previewTurret != null)
                {
                    Destroy(previewTurret.gameObject);
                }

                // Reset selection
                var previousTurret = selectedTurret;
                selectedTurret = null;
                previewTurret = null;
                hasSelectedTurret = false;

                // Notify selection change
                if (previousTurret != null)
                {
                    OnTurretSelectionChanged?.Invoke(null);
                }

                // Reset cursor
                if (cursorController != null)
                {
                    cursorController.SetDefaultCursor();
                }
            }
        }

        private void UpdatePreview()
        {
            if (previewTurret == null)
                return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, planetLayer))
            {
                if (hit.collider.CompareTag("Planet"))
                {
                    // Get direction towards planet center (opposite of up direction)
                    Vector3 upDirection = (hit.point - hit.transform.position).normalized;
                    Vector3 downDirection = -upDirection;  // Direction towards planet center
                    
                    // Place below surface by moving towards planet center
                    Vector3 position = hit.point + (downDirection * Mathf.Abs(placementOffset));
                    
                    previewTurret.transform.position = position;
                    previewTurret.transform.up = upDirection;  // Keep turret oriented away from planet
                    
                    // Check distance from other turrets
                    isValidPlacement = IsTurretPlacementValid(position);
                    UpdatePreviewColor(isValidPlacement);
                }
                else
                {
                    isValidPlacement = false;
                    UpdatePreviewColor(isValidPlacement);
                }
            }
            else
            {
                isValidPlacement = false;
                UpdatePreviewColor(isValidPlacement);
            }
        }

        private bool IsTurretPlacementValid(Vector3 position)
        {
            // Find all turrets in the scene
            DeployableBase[] existingTurrets = FindObjectsOfType<DeployableBase>();

            // Check distance from each existing turret
            foreach (var turret in existingTurrets)
            {
                if (turret == previewTurret) // Skip the preview turret
                    continue;

                float distance = Vector3.Distance(position, turret.transform.position);
                if (distance < minimumTurretDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private void PlaceTurret()
        {
            if (!hasSelectedTurret || selectedTurret == null || previewTurret == null)
                return;

            // Check if we can still afford the turret
            int cost = selectedTurret.M_TurretStats.GetCoinCost();
            if (!gameState.TrySpendCurrency(cost))
            {
                // If we can't afford it anymore, cancel placement
                CancelTurretPlacement();
                // TODO: Show cannot afford message
                return;
            }

            // Create the actual turret at the preview location and rotation
            var placedTurret = Instantiate(selectedTurret, previewTurret.transform.position, previewTurret.transform.rotation);
            
            // Clean up and select new turret for continuous placement
            var turretToReselect = selectedTurret;
            CancelTurretPlacement();
        }

        private void SetupPreviewTurret(DeployableBase preview)
        {
            // Disable components that shouldn't be active in preview
            var colliders = preview.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }

            // Make it semi-transparent
            var renderers = preview.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var materials = renderer.materials;
                foreach (var material in materials)
                {
                    material.color = new Color(material.color.r, material.color.g, material.color.b, 0.5f);
                }
            }
        }

        private void UpdatePreviewColor(bool isValid)
        {
            if (previewTurret == null)
                return;

            Color targetColor = isValid ? validPlacementColor : invalidPlacementColor;
            
            var renderers = previewTurret.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var materials = renderer.materials;
                foreach (var material in materials)
                {
                    material.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0.5f);
                }
            }
        }
    }
}
