using UnityEngine;
using DG.Tweening;

namespace Planetarium.Core.Camera
{
    public class CameraStateService : SceneService
    {
        [Header("General Settings")]
        [SerializeField] private float transitionDuration = 1f;
        [SerializeField] private Ease transitionEase = Ease.InOutSine;

        [Header("Orbit State Settings")]
        [SerializeField] private float orbitRadius = 100f;
        [SerializeField] private float orbitHeight = 20f;
        [SerializeField] private float orbitSpeed = 0.1f;
        [SerializeField] private float bobAmplitude = 5f;
        [SerializeField] private float bobFrequency = 0.5f;

        [Header("Interactive State Settings")]
        [SerializeField] private float floatSpeed = 2f;
        [SerializeField] private float dragSpeed = 0.5f;
        [SerializeField] private float mouseLookInfluence = 0.3f;
        [SerializeField] private float returnToCenterSpeed = 1f;
        [SerializeField] private float boundaryRadius = 100f;
        [SerializeField] private float minDistanceToFocus = 20f;
        [SerializeField] private float maxDistanceToFocus = 150f;
        [SerializeField] private float touchDragSpeed = 0.5f;
        [SerializeField] private float touchZoomSpeed = 0.02f;

        private UnityEngine.Camera mainCamera;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Vector3 focusPoint;
        private CameraState currentState = CameraState.None;
        private bool isTransitioning;

        // Orbit state variables
        private float currentOrbitAngle;
        private Vector3 orbitPosition;
        private bool isOrbiting;

        // Interactive state variables
        private Vector3 currentVelocity;
        private Vector2 lastMousePosition;
        private Vector2 lastTouchPosition;
        private float initialTouchDistance;
        private bool isTouching;
        private bool isDragging;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private float currentDistanceToFocus;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            mainCamera = Context.MainCamera;
            if (mainCamera == null)
            {
                Debug.LogError("Main camera not found in SceneContext!");
                return;
            }

            initialPosition = mainCamera.transform.position;
            initialRotation = mainCamera.transform.rotation;
            currentDistanceToFocus = Vector3.Distance(initialPosition, Vector3.zero);
        }

        protected override void OnTick()
        {
            base.OnTick();

            switch (currentState)
            {
                case CameraState.Orbit:
                    UpdateOrbitState();
                    break;
                case CameraState.Interactive:
                    UpdateInteractiveState();
                    break;
            }
        }

        public void TransitionToState(CameraState newState, Vector3 focusPosition)
        {
            if (isTransitioning || currentState == newState || !mainCamera) return;

            isTransitioning = true;
            focusPoint = focusPosition;
            currentState = newState;

            // Stop any existing tweens
            DOTween.Kill(mainCamera.transform);

            switch (newState)
            {
                case CameraState.Orbit:
                    TransitionToOrbitState();
                    break;
                case CameraState.Interactive:
                    TransitionToInteractiveState();
                    break;
                case CameraState.None:
                    ReturnToInitialState();
                    break;
            }
        }

        private void TransitionToOrbitState()
        {
            currentOrbitAngle = Random.Range(0f, 360f);
            UpdateOrbitPosition();

            mainCamera.transform.DOMove(orbitPosition, transitionDuration)
                .SetEase(transitionEase)
                .OnComplete(() => {
                    isTransitioning = false;
                    isOrbiting = true;
                });

            Quaternion targetRotation = Quaternion.LookRotation(focusPoint - orbitPosition);
            mainCamera.transform.DORotateQuaternion(targetRotation, transitionDuration)
                .SetEase(transitionEase);
        }

        private void TransitionToInteractiveState()
        {
            Vector3 targetPosition = focusPoint - mainCamera.transform.forward * currentDistanceToFocus;
            currentVelocity = Vector3.zero;
            
            mainCamera.transform.DOMove(targetPosition, transitionDuration)
                .SetEase(transitionEase)
                .OnComplete(() => {
                    isTransitioning = false;
                    currentDistanceToFocus = Vector3.Distance(targetPosition, focusPoint);
                });

            Quaternion targetRotation = Quaternion.LookRotation(focusPoint - targetPosition);
            mainCamera.transform.DORotateQuaternion(targetRotation, transitionDuration)
                .SetEase(transitionEase);
        }

        private void ReturnToInitialState()
        {
            mainCamera.transform.DOMove(initialPosition, transitionDuration)
                .SetEase(transitionEase)
                .OnComplete(() => {
                    isTransitioning = false;
                    isOrbiting = false;
                });

            mainCamera.transform.DORotateQuaternion(initialRotation, transitionDuration)
                .SetEase(transitionEase);
        }

        private void UpdateOrbitState()
        {
            if (!isOrbiting || isTransitioning) return;

            // Update orbit angle
            currentOrbitAngle += orbitSpeed * Time.deltaTime;
            if (currentOrbitAngle >= 360f) currentOrbitAngle -= 360f;

            UpdateOrbitPosition();

            // Apply bob effect
            Vector3 bobOffset = Vector3.up * Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            Vector3 targetPosition = orbitPosition + bobOffset;
            Quaternion targetRotation = Quaternion.LookRotation(focusPoint - targetPosition);

            // Smoothly move camera
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, Time.deltaTime * 2f);
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, targetRotation, Time.deltaTime * 2f);
        }

        private void UpdateInteractiveState()
        {
            if (isTransitioning) return;

            // Handle input based on platform
            #if UNITY_IOS || UNITY_ANDROID
                HandleTouchInput();
            #else
                HandleMouseInput();
            #endif

            // Update camera position with smooth floating motion
            UpdateCameraFloating();
        }

        private void HandleTouchInput()
        {
            // Single touch for movement
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        lastTouchPosition = touch.position;
                        isTouching = true;
                        break;

                    case TouchPhase.Moved:
                        if (isTouching)
                        {
                            Vector2 delta = touch.position - lastTouchPosition;
                            Vector3 right = mainCamera.transform.right * (-delta.x * touchDragSpeed);
                            Vector3 up = mainCamera.transform.up * (-delta.y * touchDragSpeed);
                            currentVelocity += (right + up) * Time.deltaTime;
                        }
                        lastTouchPosition = touch.position;
                        break;

                    case TouchPhase.Ended:
                        isTouching = false;
                        break;
                }
            }
            // Two finger pinch for zoom
            else if (Input.touchCount == 2)
            {
                Touch touch1 = Input.GetTouch(0);
                Touch touch2 = Input.GetTouch(1);

                if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
                {
                    initialTouchDistance = Vector2.Distance(touch1.position, touch2.position);
                }
                else if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
                {
                    float currentTouchDistance = Vector2.Distance(touch1.position, touch2.position);
                    float deltaDistance = currentTouchDistance - initialTouchDistance;
                    
                    // Adjust distance to focus
                    currentDistanceToFocus = Mathf.Clamp(
                        currentDistanceToFocus - deltaDistance * touchZoomSpeed,
                        minDistanceToFocus,
                        maxDistanceToFocus
                    );
                    
                    initialTouchDistance = currentTouchDistance;
                }
            }
        }

        private void HandleMouseInput()
        {
            // Handle dragging with left mouse button
            if (Input.GetMouseButtonDown(0))
            {
                lastMousePosition = Input.mousePosition;
                isDragging = true;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Confined;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            if (isDragging)
            {
                // Calculate mouse delta and update velocity
                Vector2 mouseDelta = (Vector2)Input.mousePosition - lastMousePosition;
                Vector3 right = mainCamera.transform.right * (-mouseDelta.x * dragSpeed);
                Vector3 up = mainCamera.transform.up * (-mouseDelta.y * dragSpeed);
                currentVelocity += (right + up) * Time.deltaTime;
                lastMousePosition = Input.mousePosition;
            }
            else
            {
                // Only apply mouse position influence when not dragging
                Vector2 mouseViewportPos = mainCamera.ScreenToViewportPoint(Input.mousePosition);
                Vector2 mouseInfluence = new Vector2(
                    (mouseViewportPos.x - 0.5f) * 2f,
                    (mouseViewportPos.y - 0.5f) * 2f
                );

                // Add subtle velocity based on mouse position
                currentVelocity += new Vector3(
                    mouseInfluence.x * mouseLookInfluence,
                    mouseInfluence.y * mouseLookInfluence,
                    0
                ) * Time.deltaTime;
            }

            // Handle zoom with mouse wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentDistanceToFocus = Mathf.Clamp(
                    currentDistanceToFocus - scroll * touchZoomSpeed * 10f,
                    minDistanceToFocus,
                    maxDistanceToFocus
                );
            }
        }

        private void UpdateCameraFloating()
        {
            // Apply floating movement with increased responsiveness
            targetPosition = mainCamera.transform.position + currentVelocity;

            // Keep within boundary
            Vector3 directionToFocus = targetPosition - focusPoint;
            float distanceToFocus = directionToFocus.magnitude;

            if (distanceToFocus > boundaryRadius)
            {
                targetPosition = focusPoint + directionToFocus.normalized * boundaryRadius;
                currentVelocity = Vector3.zero;
            }

            // Maintain minimum and maximum distance
            directionToFocus = targetPosition - focusPoint;
            distanceToFocus = directionToFocus.magnitude;
            
            if (distanceToFocus < minDistanceToFocus || distanceToFocus > maxDistanceToFocus)
            {
                float targetDistance = Mathf.Clamp(distanceToFocus, minDistanceToFocus, maxDistanceToFocus);
                targetPosition = focusPoint + directionToFocus.normalized * targetDistance;
            }

            // Apply stronger damping when dragging stops
            float dampingFactor = isDragging ? 0.5f : returnToCenterSpeed;
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, dampingFactor * Time.deltaTime);

            // Update position with improved responsiveness
            mainCamera.transform.position = Vector3.Lerp(
                mainCamera.transform.position,
                targetPosition,
                floatSpeed * Time.deltaTime
            );

            // Always look at focus point with smooth rotation
            targetRotation = Quaternion.LookRotation(focusPoint - mainCamera.transform.position);
            mainCamera.transform.rotation = Quaternion.Slerp(
                mainCamera.transform.rotation,
                targetRotation,
                floatSpeed * Time.deltaTime
            );
        }

        private void OnDisable()
        {
            // Ensure cursor is visible when disabled
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void OnDestroy()
        {
           
            // Ensure cursor is visible when destroyed
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void UpdateOrbitPosition()
        {
            float x = Mathf.Cos(currentOrbitAngle * Mathf.Deg2Rad) * orbitRadius;
            float z = Mathf.Sin(currentOrbitAngle * Mathf.Deg2Rad) * orbitRadius;
            orbitPosition = focusPoint + new Vector3(x, orbitHeight, z);
        }
    }

    public enum CameraState
    {
        None,
        Orbit,
        Interactive
    }
}
