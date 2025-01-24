using System.Collections.Generic;
using UnityEngine;
using Planetarium;

public class TurretManager : MonoBehaviour
{
    private static TurretManager instance;
    public static TurretManager Instance => instance;

    [System.Serializable]
    public class TurretData
    {
        public string turretName;
        public GameObject turretPrefab;
        public int cost;
    }

    [Header("Turret Settings")]
    [SerializeField] private List<TurretData> availableTurrets;
    [SerializeField] private LayerMask placementSurface;
    [SerializeField] private float placementOffset = 0f; // Offset from surface if needed
    private PlanetBase planet;

    private GameObject currentTurretPreview;
    private TurretData selectedTurret;
    private bool isPlacing;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            planet = FindFirstObjectByType<PlanetBase>();
            if (planet == null)
            {
                Debug.LogError("No planet found in scene!");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SelectTurret(string turretName)
    {
        selectedTurret = availableTurrets.Find(t => t.turretName == turretName);
        if (selectedTurret != null)
        {
            StartPlacement();
        }
    }

    private void StartPlacement()
    {
        if (currentTurretPreview != null)
        {
            Destroy(currentTurretPreview);
        }

        currentTurretPreview = Instantiate(selectedTurret.turretPrefab);
        // Set preview material or shader here if needed
        isPlacing = true;
    }

    private void Update()
    {
        if (!isPlacing || currentTurretPreview == null || planet == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementSurface))
        {
            // Calculate up direction from planet to hit point
            Vector3 upDirection = (hit.point - planet.transform.position).normalized;
            
            // Position exactly at hit point
            Vector3 position = hit.point;
            
            // Set position and rotate to align with planet surface
            currentTurretPreview.transform.position = position;
            currentTurretPreview.transform.up = upDirection;

            if (Input.GetMouseButtonDown(0) && CanPlaceTurret(position))
            {
                PlaceTurret(position, currentTurretPreview.transform.rotation);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
        }
    }

    private bool CanPlaceTurret(Vector3 position)
    {
        // Implement placement validation logic here
        // Check for overlapping turrets, valid placement surface, etc.
        return true;
    }

    private void PlaceTurret(Vector3 position, Quaternion rotation)
    {
        GameObject newTurret = Instantiate(selectedTurret.turretPrefab, position, rotation);
        
        // Ensure the placed turret is aligned with the planet surface
        Vector3 upDirection = (position - planet.transform.position).normalized;
        newTurret.transform.up = upDirection;

        CancelPlacement();
    }

    private void CancelPlacement()
    {
        if (currentTurretPreview != null)
        {
            Destroy(currentTurretPreview);
        }
        isPlacing = false;
        selectedTurret = null;
    }

    public List<TurretData> GetAvailableTurrets()
    {
        return availableTurrets;
    }
}
