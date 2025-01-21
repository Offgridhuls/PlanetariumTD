using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private float placementHeight = 0.5f;

    private GameObject currentTurretPreview;
    private TurretData selectedTurret;
    private bool isPlacing;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
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
        if (!isPlacing || currentTurretPreview == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementSurface))
        {
            Vector3 position = hit.point + Vector3.up * placementHeight;
            currentTurretPreview.transform.position = position;

            if (Input.GetMouseButtonDown(0) && CanPlaceTurret(position))
            {
                PlaceTurret(position);
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

    private void PlaceTurret(Vector3 position)
    {
        GameObject newTurret = Instantiate(selectedTurret.turretPrefab, position, Quaternion.identity);
        // Additional setup for the placed turret if needed

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
