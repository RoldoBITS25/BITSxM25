using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerGame
{
    /// <summary>
    /// Top-down camera controller with optional pan and zoom
    /// Attach to the main camera for enhanced camera control
    /// </summary>
    public class TopDownCamera : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private bool enableCameraControl = false;
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector3 offset = new Vector3(0, 20, -15);
        
        [Header("Pan Settings")]
        [SerializeField] private bool enablePan = true;
        [SerializeField] private float panSpeed = 10f;
        [SerializeField] private float panBorderThickness = 10f;
        [SerializeField] private Vector2 panLimit = new Vector2(30f, 30f);
        
        [Header("Zoom Settings")]
        [SerializeField] private bool enableZoom = true;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minZoom = 10f;
        [SerializeField] private float maxZoom = 40f;
        
        [Header("Rotation Settings")]
        [SerializeField] private bool enableRotation = false;
        [SerializeField] private float rotationSpeed = 100f;
        
        [Header("Smoothing")]
        [SerializeField] private bool enableSmoothing = true;
        [SerializeField] private float smoothSpeed = 5f;

        private Vector3 targetPosition;
        private float currentZoom;
        private float targetZoom;
        private float currentRotation;
        private Camera cam;
        private Vector2 panInput;
        private float zoomInput;
        private float rotationInput;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = gameObject.AddComponent<Camera>();
            }
            
            targetPosition = transform.position;
            currentZoom = offset.y;
            targetZoom = currentZoom;
            currentRotation = transform.eulerAngles.y;
        }

        private void Start()
        {
            if (followTarget != null)
            {
                UpdateCameraPosition();
            }
        }

        private void Update()
        {
            if (!enableCameraControl)
            {
                // Just follow target if camera control is disabled
                if (followTarget != null)
                {
                    UpdateCameraPosition();
                }
                return;
            }

            HandlePanInput();
            HandleZoomInput();
            HandleRotationInput();
            HandleEdgePan();
            
            UpdateCameraPosition();
        }

        private void HandlePanInput()
        {
            if (!enablePan) return;

            // WASD or Arrow keys for panning
            Vector3 moveDirection = Vector3.zero;
            
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                    moveDirection += Vector3.forward;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                    moveDirection += Vector3.back;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    moveDirection += Vector3.left;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    moveDirection += Vector3.right;
            }

            if (moveDirection != Vector3.zero)
            {
                // Apply rotation to movement direction
                moveDirection = Quaternion.Euler(0, currentRotation, 0) * moveDirection;
                targetPosition += moveDirection.normalized * panSpeed * Time.deltaTime;
                targetPosition = ClampPosition(targetPosition);
            }
        }

        private void HandleEdgePan()
        {
            if (!enablePan) return;

            Vector3 moveDirection = Vector3.zero;
            
            if (Mouse.current != null)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                
                // Top edge
                if (mousePos.y >= Screen.height - panBorderThickness)
                    moveDirection += Vector3.forward;
                // Bottom edge
                if (mousePos.y <= panBorderThickness)
                    moveDirection += Vector3.back;
                // Right edge
                if (mousePos.x >= Screen.width - panBorderThickness)
                    moveDirection += Vector3.right;
                // Left edge
                if (mousePos.x <= panBorderThickness)
                    moveDirection += Vector3.left;
            }

            if (moveDirection != Vector3.zero)
            {
                moveDirection = Quaternion.Euler(0, currentRotation, 0) * moveDirection;
                targetPosition += moveDirection.normalized * panSpeed * Time.deltaTime;
                targetPosition = ClampPosition(targetPosition);
            }
        }

        private void HandleZoomInput()
        {
            if (!enableZoom) return;

            float scroll = 0f;
            
            if (Mouse.current != null)
            {
                scroll = Mouse.current.scroll.ReadValue().y;
            }

            if (scroll != 0f)
            {
                targetZoom -= scroll * zoomSpeed * 0.1f;
                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            }
        }

        private void HandleRotationInput()
        {
            if (!enableRotation) return;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.qKey.isPressed)
                    currentRotation -= rotationSpeed * Time.deltaTime;
                if (Keyboard.current.eKey.isPressed)
                    currentRotation += rotationSpeed * Time.deltaTime;
            }
        }

        private void UpdateCameraPosition()
        {
            // Update zoom
            if (enableSmoothing)
            {
                currentZoom = Mathf.Lerp(currentZoom, targetZoom, smoothSpeed * Time.deltaTime);
            }
            else
            {
                currentZoom = targetZoom;
            }

            // Calculate camera position based on zoom
            Vector3 zoomedOffset = offset.normalized * currentZoom;
            
            Vector3 desiredPosition;
            if (followTarget != null && !enableCameraControl)
            {
                desiredPosition = followTarget.position + zoomedOffset;
            }
            else
            {
                desiredPosition = targetPosition;
                desiredPosition.y = currentZoom;
            }

            // Apply smoothing
            if (enableSmoothing)
            {
                transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            }
            else
            {
                transform.position = desiredPosition;
            }

            // Look at the target point
            Vector3 lookAtPoint = transform.position;
            lookAtPoint.y = 0;
            transform.LookAt(lookAtPoint);
            
            // Apply rotation
            if (enableRotation)
            {
                transform.RotateAround(transform.position, Vector3.up, currentRotation);
            }
        }

        private Vector3 ClampPosition(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, -panLimit.x, panLimit.x);
            position.z = Mathf.Clamp(position.z, -panLimit.y, panLimit.y);
            return position;
        }

        /// <summary>
        /// Set the target for the camera to follow
        /// </summary>
        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
            if (target != null)
            {
                targetPosition = target.position;
            }
        }

        /// <summary>
        /// Enable or disable camera control
        /// </summary>
        public void SetCameraControlEnabled(bool enabled)
        {
            enableCameraControl = enabled;
        }

        /// <summary>
        /// Reset camera to default position
        /// </summary>
        public void ResetCamera()
        {
            targetPosition = Vector3.zero;
            targetZoom = offset.y;
            currentRotation = 0f;
        }

        /// <summary>
        /// Focus camera on a specific position
        /// </summary>
        public void FocusOnPosition(Vector3 position, float zoom = -1f)
        {
            targetPosition = position;
            if (zoom > 0)
            {
                targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            }
        }

        private void OnDrawGizmos()
        {
            // Draw pan limits
            Gizmos.color = Color.cyan;
            Vector3 center = Vector3.zero;
            Vector3 size = new Vector3(panLimit.x * 2f, 0.1f, panLimit.y * 2f);
            Gizmos.DrawWireCube(center, size);

            // Draw camera target position
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(targetPosition, 0.5f);
            }
        }

        #region Input System Actions (Optional)
        
        // These can be called from Input System if you prefer that over direct keyboard access
        
        public void OnPan(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                panInput = context.ReadValue<Vector2>();
            }
            else if (context.canceled)
            {
                panInput = Vector2.zero;
            }
        }

        public void OnZoom(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                zoomInput = context.ReadValue<float>();
                targetZoom -= zoomInput * zoomSpeed;
                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            }
        }

        public void OnRotate(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                rotationInput = context.ReadValue<float>();
            }
            else if (context.canceled)
            {
                rotationInput = 0f;
            }
        }
        
        #endregion
    }
}
