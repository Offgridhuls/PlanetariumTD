using UnityEngine;

namespace Planetarium
{
    public class CameraControlService : SceneService
    {
        [Header("Camera Settings")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float rotationSpeed = 220f;
        [SerializeField] private float focusDistance = 1f;
        [SerializeField] private Vector3 pivot;

        [Header("Zoom Settings")]
        [SerializeField] private float mouseZoomSpeed = 0.02f;
        [SerializeField] private float touchZoomSpeed = 0.01f;
        [SerializeField] private float zoomSensitivity = 1f;
        [SerializeField] private float minZoom = 10f;
        [SerializeField] private float maxZoom = 50f;
        [SerializeField] private float currentZoom;

        private Camera mainCamera;
        private Vector3 mousePosOld;
        private bool hasFocusOld;
        private Vector3 lastCtrlPivot;
        private Vector2 rightClickPos;
        private Vector3 startPosition;
        private Quaternion startRotation;

        // Touch support
        private Vector2 touchStartPos;
        private float initialPinchDistance;
        private bool wasZooming;

        protected override void OnInitialize()
        {
            mainCamera = Context.MainCamera;
            
            if (mainCamera == null)
            {
                Debug.LogError("Main camera not found in SceneContext!");
                return;
            }

            startPosition = mainCamera.transform.position;
            startRotation = mainCamera.transform.rotation;
            currentZoom = Vector3.Distance(startPosition, pivot);
            
            Debug.Log("Camera Controls: One finger to Orbit, Two fingers to zoom/pan");
            
            base.OnInitialize();
        }

        protected override void OnTick()
        {
            if (mainCamera == null || !IsActive || Input.GetKey(KeyCode.LeftShift)) return;

            if (Application.isFocused != hasFocusOld)
            {
                hasFocusOld = Application.isFocused;
                mousePosOld = Input.mousePosition;
                touchStartPos = Input.mousePosition;
            }

            // Update current zoom
            currentZoom = Vector3.Distance(mainCamera.transform.position, pivot);

            // Handle touch input
            if (Input.touchCount > 0)
            {
                HandleTouchInput();
                return; // Skip mouse input when using touch
            }

            // Handle mouse input
            HandleMouseInput();
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    lastCtrlPivot = mainCamera.transform.position + mainCamera.transform.forward * focusDistance;
                }

                // Single finger orbit
                if (touch.phase == TouchPhase.Moved)
                {
                    float mouseMoveX = touch.deltaPosition.x / Screen.width;
                    float mouseMoveY = touch.deltaPosition.y / Screen.width;

                    mainCamera.transform.RotateAround(pivot, mainCamera.transform.right, mouseMoveY * -rotationSpeed);
                    mainCamera.transform.RotateAround(pivot, Vector3.up, mouseMoveX * rotationSpeed);
                }
            }
            else if (Input.touchCount == 2)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);

                if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
                {
                    initialPinchDistance = Vector2.Distance(touch0.position, touch1.position);
                    wasZooming = false;
                }
                else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
                {
                    // Calculate the current distance between touches
                    float currentPinchDistance = Vector2.Distance(touch0.position, touch1.position);

                    if (!wasZooming)
                    {
                        initialPinchDistance = currentPinchDistance;
                        wasZooming = true;
                    }

                    // Determine if this is primarily a zoom or pan gesture
                    Vector2 touch0Delta = touch0.deltaPosition;
                    Vector2 touch1Delta = touch1.deltaPosition;
                    bool isSameDirection = Vector2.Dot(touch0Delta.normalized, touch1Delta.normalized) > 0.8f;

                    if (isSameDirection)
                    {
                        // Pan - both fingers moving in same direction
                        Vector2 averageDelta = (touch0Delta + touch1Delta) * 0.5f;
                        float dstWeight = currentZoom;
                        Vector3 move = Vector3.zero;
                        
                        move += Vector3.up * (-averageDelta.y / Screen.width) * -moveSpeed * dstWeight;
                        move += Vector3.right * (-averageDelta.x / Screen.width) * -moveSpeed * dstWeight;
                        
                        mainCamera.transform.Translate(move);
                    }
                    else
                    {
                        // Zoom - pinch gesture
                        float deltaPinchDistance = currentPinchDistance - initialPinchDistance;
                        float dstWeight = currentZoom;
                        
                        // Calculate potential new position
                        float zoomAmount = deltaPinchDistance * touchZoomSpeed * dstWeight * 0.01f * zoomSensitivity;
                        Vector3 zoomMove = mainCamera.transform.forward * zoomAmount;
                        Vector3 newPosition = mainCamera.transform.position + zoomMove;
                        float newZoom = Vector3.Distance(newPosition, pivot);

                        // Only apply if within limits
                        if (newZoom >= minZoom && newZoom <= maxZoom)
                        {
                            mainCamera.transform.position = newPosition;
                            currentZoom = newZoom;
                        }
                        
                        initialPinchDistance = currentPinchDistance;
                    }
                }
            }
        }

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                lastCtrlPivot = mainCamera.transform.position + mainCamera.transform.forward * focusDistance;
            }

            float dstWeight = currentZoom;
            Vector2 mouseMove = Input.mousePosition - mousePosOld;
            mousePosOld = Input.mousePosition;
            float mouseMoveX = mouseMove.x / Screen.width;
            float mouseMoveY = mouseMove.y / Screen.width;
            Vector3 move = Vector3.zero;

            if (Input.GetMouseButton(2))
            {
                move += Vector3.up * mouseMoveY * -moveSpeed * dstWeight;
                move += Vector3.right * mouseMoveX * -moveSpeed * dstWeight;
            }

            if (Input.GetMouseButton(0))
            {
                mainCamera.transform.RotateAround(pivot, mainCamera.transform.right, mouseMoveY * -rotationSpeed);
                mainCamera.transform.RotateAround(pivot, Vector3.up, mouseMoveX * rotationSpeed);
            }

            // Apply movement
            mainCamera.transform.Translate(move);

            // Handle zooming
            float scrollDelta = Input.mouseScrollDelta.y;
            if (scrollDelta != 0)
            {
                float zoomAmount = scrollDelta * mouseZoomSpeed * dstWeight * zoomSensitivity;
                Vector3 zoomMove = mainCamera.transform.forward * zoomAmount;
                Vector3 newPosition = mainCamera.transform.position + zoomMove;
                float newZoom = Vector3.Distance(newPosition, pivot);

                if (newZoom >= minZoom && newZoom <= maxZoom)
                {
                    mainCamera.transform.position = newPosition;
                    currentZoom = newZoom;
                }
            }
        }

        public void ResetCamera()
        {
            mainCamera.transform.position = startPosition;
            mainCamera.transform.rotation = startRotation;
            currentZoom = Vector3.Distance(startPosition, pivot);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pivot, 0.5f);
        }
    }
}
