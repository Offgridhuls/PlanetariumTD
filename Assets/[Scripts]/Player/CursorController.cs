using UnityEngine;

namespace Planetarium
{
    public class CursorController : MonoBehaviour
    {
        [Header("Cursor Settings")]
        [SerializeField] private Texture2D defaultCursor;
        [SerializeField] private Texture2D placementCursor;
        [SerializeField] private Vector2 cursorHotspot = Vector2.zero;

        private void Start()
        {
            // Set initial cursor
            SetDefaultCursor();
            
            // Ensure cursor is always visible
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void SetDefaultCursor()
        {
            if (defaultCursor != null)
            {
                Cursor.SetCursor(defaultCursor, cursorHotspot, CursorMode.Auto);
            }
        }

        public void SetPlacementCursor()
        {
            if (placementCursor != null)
            {
                Cursor.SetCursor(placementCursor, cursorHotspot, CursorMode.Auto);
            }
        }

        public bool IsCursorActive()
        {
            return Cursor.visible;
        }

        private void OnDisable()
        {
            // Reset to default system cursor
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
