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
        if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) // Mouse and Keyboard
        {
            Debug.Log("MouseButtonDown");
            Vector3 mouse = Input.mousePosition;
            if (CursorController.IsCursorActive() && bhasSelectedTurret)
            {
                TryPlaceTurret(mouse);
            }
        }
        else if(Input.touchCount > 0) // Touches
        {
            Touch touch = Input.GetTouch(0);
            if(bhasSelectedTurret)
            {
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        TryPlaceTurret(touch.position);
                        break;

                    case TouchPhase.Moved:
                        break;

                    case TouchPhase.Stationary:
                        break;

                    case TouchPhase.Ended:
                        break;

                    case TouchPhase.Canceled:
                        break;
                }
            }  
        }
    }
    public void EnableSelectedTurret(DeployableBase selectedTurret)
    {
        if (!bhasSelectedTurret)
        {
            DisableSelectedTurret();
            SelectedTurret = selectedTurret;
            bhasSelectedTurret = true;
        }
    }

    public void DisableSelectedTurret()
    {
        if (bhasSelectedTurret && SelectedTurret != null)
        {
            SelectedTurret = null;
            bhasSelectedTurret = false;
        }
    }

    void TryPlaceTurret(Vector3 ScreenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(ScreenPosition);
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
        //CursorController.HideCursor();
    }
}
