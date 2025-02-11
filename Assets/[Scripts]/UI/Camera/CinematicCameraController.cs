using UnityEngine;
using DG.Tweening;

namespace Planetarium.UI.Camera
{
    public class CinematicCameraController : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform focusPoint;
        [SerializeField] private float orbitRadius = 100f;
        [SerializeField] private float heightOffset = 20f;

        [Header("Movement Settings")]
        [SerializeField] private float orbitSpeed = 0.1f;
        [SerializeField] private float bobAmplitude = 5f;
        [SerializeField] private float bobFrequency = 0.5f;
        
        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 2f;
        [SerializeField] private Ease transitionEase = Ease.InOutSine;

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Vector3 currentOrbitPosition;
        private float currentOrbitAngle;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool isOrbiting = false;

        private void Awake()
        {
            // Store initial transform
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        private void Start()
        {
            if (focusPoint == null)
            {
                focusPoint = new GameObject("CameraFocusPoint").transform;
                focusPoint.position = Vector3.zero;
            }

            // Initialize position
            currentOrbitAngle = Random.Range(0f, 360f);
            UpdateOrbitPosition();
        }

        private void Update()
        {
            if (!isOrbiting) return;

            // Update orbit angle
            currentOrbitAngle += orbitSpeed * Time.deltaTime;
            if (currentOrbitAngle >= 360f) currentOrbitAngle -= 360f;

            UpdateOrbitPosition();

            // Apply bob effect
            Vector3 bobOffset = Vector3.up * Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            targetPosition = currentOrbitPosition + bobOffset;
            targetRotation = Quaternion.LookRotation(focusPoint.position - targetPosition);

            // Smoothly move camera
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 2f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
        }

        private void UpdateOrbitPosition()
        {
            float x = Mathf.Cos(currentOrbitAngle * Mathf.Deg2Rad) * orbitRadius;
            float z = Mathf.Sin(currentOrbitAngle * Mathf.Deg2Rad) * orbitRadius;
            currentOrbitPosition = focusPoint.position + new Vector3(x, heightOffset, z);
        }

        public void StartCinematicMode(Vector3 focusPosition)
        {
            focusPoint.position = focusPosition;
            
            // Calculate initial target position
            UpdateOrbitPosition();
            Vector3 targetPos = currentOrbitPosition;

            // Stop any existing tweens
            DOTween.Kill(transform);

            // Animate to starting position
            transform.DOMove(targetPos, transitionDuration)
                .SetEase(transitionEase)
                .OnComplete(() => isOrbiting = true);

            // Calculate target rotation
            Quaternion targetRot = Quaternion.LookRotation(focusPoint.position - targetPos);
            
            // Animate rotation
            transform.DORotateQuaternion(targetRot, transitionDuration)
                .SetEase(transitionEase);
        }

        public void StopCinematicMode()
        {
            isOrbiting = false;

            // Stop any existing tweens
            DOTween.Kill(transform);

            // Return to initial position and rotation
            transform.DOMove(initialPosition, transitionDuration)
                .SetEase(transitionEase);
            
            transform.DORotateQuaternion(initialRotation, transitionDuration)
                .SetEase(transitionEase);
        }

        public void SetFocusPoint(Vector3 position)
        {
            focusPoint.position = position;
        }

        public void SetOrbitParameters(float radius, float height, float speed)
        {
            orbitRadius = radius;
            heightOffset = height;
            orbitSpeed = speed;
        }
    }
}
