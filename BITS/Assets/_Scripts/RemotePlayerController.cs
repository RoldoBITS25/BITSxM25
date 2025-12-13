using UnityEngine;

namespace MultiplayerGame
{
    /// <summary>
    /// Controls a remote player character
    /// Interpolates position and rotation for smooth movement
    /// </summary>
    public class RemotePlayerController : MonoBehaviour
    {
        [Header("Smoothing")]
        [SerializeField] private float positionSmoothTime = 0.1f;
        [SerializeField] private float rotationSmoothTime = 0.1f;
        [SerializeField] private float snapDistance = 5f; // Distance at which we teleport instead of smoothing

        private string playerId;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        
        // Smoothing velocity references
        private Vector3 currentVelocity;
        private float currentRotVelocity; // Not used for Slerp but kept for reference if we switch to SmoothDampAngle

        private bool isInitialized = false;

        public void Initialize(string id)
        {
            playerId = id;
            targetPosition = transform.position;
            targetRotation = transform.rotation;
            isInitialized = true;
        }

        public void SetTarget(Vector3 position, Quaternion rotation)
        {
            if (!isInitialized) return;

            targetPosition = position;
            targetRotation = rotation;

            // If the distance is too large, snap immediately to avoid weird sliding across the map
            if (Vector3.Distance(transform.position, targetPosition) > snapDistance)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }
        }

        public void SetTarget(Vector3 position)
        {
            if (!isInitialized) return;

            targetPosition = position;
            // Keep existing targetRotation

            if (Vector3.Distance(transform.position, targetPosition) > snapDistance)
            {
                transform.position = targetPosition;
                // Keep existing rotation
            }
        }

        private void Update()
        {
            if (!isInitialized) return;

            // Smoothly interpolate position
            // Vector3.SmoothDamp is generally better for network smoothing than Lerp because it handles varying update rates better
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, positionSmoothTime);

            // Smoothly interpolate rotation
            // For rotation, Slerp with a time factor is standard, or we could use RotateTowards
            // Let's use simple Slerp for now, effectively easing it over time
            float t = Time.deltaTime / rotationSmoothTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }
    }
}
