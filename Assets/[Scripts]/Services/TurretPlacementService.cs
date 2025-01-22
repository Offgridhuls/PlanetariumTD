using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace Planetarium
{
    [RequireComponent(typeof(CursorController))]
    public class TurretPlacementService : SceneService
    {
        [Header("Placement Settings")]
        [SerializeField] private float placementOffset = 0.15f;
        [SerializeField] private LayerMask planetLayer;
        [SerializeField] private Color validPlacementColor = new Color(0, 1, 0, 0.5f);
        [SerializeField] private Color invalidPlacementColor = new Color(1, 0, 0, 0.5f);

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
                    Vector3 position = hit.point + hit.normal * placementOffset;
                    Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    
                    previewTurret.transform.position = position;
                    previewTurret.transform.rotation = rotation;
                    
                    // TODO: Add additional placement validation (e.g., distance from other turrets)
                    isValidPlacement = true;
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

            // Create the actual turret at the preview location
            var placedTurret = Instantiate(selectedTurret, previewTurret.transform.position, previewTurret.transform.rotation);
            
            // Clean up and select new turret for continuous placement
            var turretToReselect = selectedTurret;
            CancelTurretPlacement();

            // Only try to select new turret if we can afford it
            if (gameState.Currency >= cost)
            {
                SelectTurret(turretToReselect);
            }
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
