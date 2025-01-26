using UnityEngine;

namespace Planetarium
{
    public class CameraControlService : SceneService
    {
        [Header("Camera Settings")]
        [SerializeField] private float panSpeed = 50f;
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float minZoom = 10f;
        [SerializeField] private float maxZoom = 30f;
        [SerializeField] private float smoothSpeed = 10f;
        
        [Header("Boundaries")]
        [SerializeField] private float maxVerticalAngle = 85f;

        private Camera mainCamera;
        private Vector2 lastTouchPosition;
        private bool isDragging;
        
        private Vector3 targetPosition;
        private float currentZoom;
        private Vector3 currentRotation;

        protected override void OnInitialize()
        {
            mainCamera = Context.MainCamera;
            
            if (mainCamera == null)
            {
                Debug.LogError("Main camera not found in SceneContext!");
                return;
            }

            // Initialize camera position
            currentZoom = Vector3.Distance(mainCamera.transform.position, Vector3.zero);
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            currentRotation = mainCamera.transform.eulerAngles;
            
            // Make camera look at planet
            mainCamera.transform.LookAt(Vector3.zero);
            targetPosition = mainCamera.transform.position;
            
            base.OnInitialize();
        }

        protected override void OnTick()
        {
            if (mainCamera == null || !IsActive) return;
            
            HandleTouchInput();
            UpdateCameraPosition();
        }

        private void HandleTouchInput()
        {
            // Handle touch input for mobile
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        isDragging = true;
                        lastTouchPosition = touch.position;
                        break;

                    case TouchPhase.Moved:
                        if (isDragging)
                        {
                            Vector2 delta = touch.position - lastTouchPosition;
                            PanCamera(delta);
                            lastTouchPosition = touch.position;
                        }
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        isDragging = false;
                        break;
                }

                // Handle pinch to zoom with two fingers
                if (Input.touchCount == 2)
                {
                    Touch touch0 = Input.GetTouch(0);
                    Touch touch1 = Input.GetTouch(1);

                    Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
                    Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

                    float prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
                    float currentMagnitude = (touch0.position - touch1.position).magnitude;

                    float difference = currentMagnitude - prevMagnitude;
                    ZoomCamera(difference * 0.01f);
                }
            }

            // Handle mouse input for testing in editor
            #if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                isDragging = true;
                lastTouchPosition = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0) && isDragging)
            {
                Vector2 delta = (Vector2)Input.mousePosition - lastTouchPosition;
                PanCamera(delta);
                lastTouchPosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }

            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
            if (scrollDelta != 0)
            {
                ZoomCamera(scrollDelta);
            }
            #endif
        }

        private void PanCamera(Vector2 delta)
        {
            float screenSizeModifier = Screen.height / 1000f;
            
            // Calculate rotation changes
            float horizontalRotation = delta.x * panSpeed * Time.deltaTime / screenSizeModifier;
            float verticalRotation = -delta.y * panSpeed * Time.deltaTime / screenSizeModifier;

            // Update rotation, clamping vertical to prevent flipping
            currentRotation.y += horizontalRotation;
            currentRotation.x = Mathf.Clamp(currentRotation.x + verticalRotation, -maxVerticalAngle, maxVerticalAngle);

            // Calculate new position based on rotation
            Vector3 direction = Quaternion.Euler(currentRotation) * Vector3.forward;
            targetPosition = -direction * currentZoom;
        }

        private void ZoomCamera(float zoomDelta)
        {
            // Update zoom level
            currentZoom = Mathf.Clamp(currentZoom - zoomDelta * zoomSpeed, minZoom, maxZoom);
            
            // Update position based on new zoom
            Vector3 direction = (mainCamera.transform.position - Vector3.zero).normalized;
            targetPosition = direction * currentZoom;
        }

        private void UpdateCameraPosition()
        {
            // Smoothly move camera to target position
            mainCamera.transform.position = Vector3.Lerp(
                mainCamera.transform.position,
                targetPosition,
                Time.deltaTime * smoothSpeed
            );

            // Always look at the planet (origin)
            mainCamera.transform.LookAt(Vector3.zero);
        }
    }
}
