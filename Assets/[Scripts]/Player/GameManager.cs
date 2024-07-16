using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private CursorController CursorController;
    private DeployableBase SelectedTurret { get; set; }
    bool bhasSelectedTurret;
    bool bCanPlaceTurret;

    [Header("Game Info")]
    [SerializeField]
    private float CurrentScrap;
    [SerializeField]
    private float CurrentCoins;

    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        CursorController = FindObjectOfType<CursorController>();
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("MouseButtonDown");
            if (CursorController.IsCursorActive() && bhasSelectedTurret)
            {
                TryPlaceTurret();
            }
        }
    }
    public void EnableSelectedTurret(DeployableBase selectedTurret)
    {
        if (!bhasSelectedTurret)
        {
            SelectedTurret = selectedTurret;
            bhasSelectedTurret = true;
        }
    }

    public void DisableSelectedTurret()
    {
        if (bhasSelectedTurret && SelectedTurret == null)
        {
            SelectedTurret = null;
            bhasSelectedTurret = false;
        }
    }

    void TryPlaceTurret()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject.tag == "Planet")
            {
                var hitPoint = hit.point;

                Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                
                PlaceTurret(hit.point + hit.normal * .15f, targetRotation);
            }
        }
    }

    void PlaceTurret(Vector3 position, Quaternion rotation)
    {
        Instantiate(SelectedTurret, new Vector3(position.x, position.y, position.z), rotation);
        DisableSelectedTurret();
        CursorController.HideCursor();
    }
}
