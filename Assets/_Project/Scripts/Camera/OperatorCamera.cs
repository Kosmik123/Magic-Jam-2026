using UnityEngine;
using UnityEngine.InputSystem;

namespace MagicJam.CameraSystem
{
    /// <summary>
    /// Free-moving film camera operated by the player.
    /// WASD moves on the horizontal plane, mouse rotates while the middle button is held, Q/E lowers/raises, scroll zooms (changes FOV).
    /// Movement is clamped to a room bounds box and blocked by colliders on the collision mask.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class OperatorCamera : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField]
        private InputActionReference moveAction;

        [SerializeField]
        private InputActionReference lookAction;

        [Tooltip("Button that must be held for mouse movement to rotate the camera.")]
        [SerializeField]
        private InputActionReference rotateAction;

        [SerializeField]
        private InputActionReference verticalAction;

        [SerializeField]
        private InputActionReference zoomAction;

        [Header("Movement")]
        [SerializeField]
        private float moveSpeed = 3f;

        [SerializeField]
        private float verticalSpeed = 2f;

        [Tooltip("Time to reach target velocity. 0 = instant.")]
        [SerializeField]
        private float moveSmoothTime = 0.12f;

        [Header("Rotation")]
        [Tooltip("Degrees per pixel of mouse movement.")]
        [SerializeField]
        private float lookSensitivity = 0.1f;

        [SerializeField]
        private float minPitch = -85f;

        [SerializeField]
        private float maxPitch = 85f;

        [Header("Zoom (FOV)")]
        [SerializeField]
        private float minFov = 15f;

        [SerializeField]
        private float maxFov = 75f;

        [Tooltip("FOV change per scroll notch.")]
        [SerializeField]
        private float zoomStep = 5f;

        [SerializeField]
        private float zoomSmoothTime = 0.1f;

        [Header("Bounds & Collision")]
        [Tooltip("Box the camera must stay inside (usually the room volume). Optional.")]
        [SerializeField]
        private BoxCollider roomBounds;

        [Tooltip("Layers the camera cannot pass through (walls, furniture).")]
        [SerializeField]
        private LayerMask collisionMask = ~0;

        [SerializeField]
        private float collisionRadius = 0.2f;

        [Tooltip("Locks and hides the cursor while the rotate button is held.")]
        [SerializeField]
        private bool lockCursor = true;

        private const float Skin = 0.01f;

        private Camera cam;
        private float yaw;
        private float pitch;
        private Vector3 velocity;
        private Vector3 velocitySmoothing;
        private float targetFov;
        private float fovSmoothing;
        private bool isRotating;

        public Camera Camera => cam;

        /// <summary>When false, input is ignored (e.g. pause menu, scripted finale).</summary>
        public bool InputEnabled { get; set; } = true;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = NormalizeAngle(euler.x);
            targetFov = Mathf.Clamp(cam.fieldOfView, minFov, maxFov);
        }

        private void OnEnable()
        {
            SetActionsEnabled(true);
        }

        private void OnDisable()
        {
            SetActionsEnabled(false);
            SetRotating(false);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            bool input = InputEnabled;

            SetRotating(input && Read<float>(rotateAction) > 0f);
            UpdateRotation(isRotating ? Read<Vector2>(lookAction) : Vector2.zero);
            UpdateZoom(input ? Read<float>(zoomAction) : 0f, dt);
            UpdateMovement(
                input ? Read<Vector2>(moveAction) : Vector2.zero,
                input ? Read<float>(verticalAction) : 0f,
                dt);
        }

        /// <summary>Instantly places the camera, e.g. at level start. Respects bounds.</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            Vector3 euler = rotation.eulerAngles;
            yaw = euler.y;
            pitch = Mathf.Clamp(NormalizeAngle(euler.x), minPitch, maxPitch);
            transform.SetPositionAndRotation(ClampToBounds(position), Quaternion.Euler(pitch, yaw, 0f));
            velocity = Vector3.zero;
            velocitySmoothing = Vector3.zero;
        }

        private void SetRotating(bool value)
        {
            if (isRotating == value)
                return;

            isRotating = value;
            if (lockCursor == false)
                return;

            Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = value == false;
        }

        private void UpdateRotation(Vector2 look)
        {
            yaw += look.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - look.y * lookSensitivity, minPitch, maxPitch);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void UpdateZoom(float scroll, float dt)
        {
            // Scroll magnitude differs per platform (1 or 120 per notch) - only direction matters.
            if (scroll != 0f)
                targetFov = Mathf.Clamp(targetFov - Mathf.Clamp(scroll, -1f, 1f) * zoomStep, minFov, maxFov);

            cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, targetFov, ref fovSmoothing, zoomSmoothTime, Mathf.Infinity, dt);
        }

        private void UpdateMovement(Vector2 move, float vertical, float dt)
        {
            // Horizontal movement follows yaw only, so looking up/down doesn't change height.
            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 targetVelocity = yawRotation * new Vector3(move.x, 0f, move.y) * moveSpeed
                                     + Vector3.up * (vertical * verticalSpeed);

            velocity = moveSmoothTime > 0f
                ? Vector3.SmoothDamp(velocity, targetVelocity, ref velocitySmoothing, moveSmoothTime, Mathf.Infinity, dt)
                : targetVelocity;

            Vector3 delta = velocity * dt;
            if (delta.sqrMagnitude < 1e-8f)
                return;

            Vector3 position = MoveWithCollision(transform.position, delta);
            Vector3 clamped = ClampToBounds(position);

            // Kill velocity along axes blocked by bounds so the camera doesn't "push" against them.
            if (clamped != position)
                velocity = Vector3.ProjectOnPlane(velocity, (position - clamped).normalized);

            transform.position = clamped;
        }

        private Vector3 MoveWithCollision(Vector3 from, Vector3 delta)
        {
            // Two passes: move until hit, then slide along the hit surface.
            for (int i = 0; i < 2; i++)
            {
                float distance = delta.magnitude;
                if (distance < 1e-5f)
                    break;

                Vector3 direction = delta / distance;
                if (!Physics.SphereCast(from, collisionRadius, direction, out RaycastHit hit, distance + Skin,
                        collisionMask, QueryTriggerInteraction.Ignore))
                {
                    return from + delta;
                }

                float allowed = Mathf.Max(0f, hit.distance - Skin);
                from += direction * allowed;
                delta = Vector3.ProjectOnPlane(direction * (distance - allowed), hit.normal);
                velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
            }

            return from;
        }

        private Vector3 ClampToBounds(Vector3 worldPosition)
        {
            if (roomBounds == null)
                return worldPosition;

            Transform t = roomBounds.transform;
            Vector3 local = t.InverseTransformPoint(worldPosition);
            Vector3 half = roomBounds.size * 0.5f;
            Vector3 scale = t.lossyScale;
            Vector3 margin = new Vector3(
                collisionRadius / Mathf.Max(Mathf.Abs(scale.x), 1e-5f),
                collisionRadius / Mathf.Max(Mathf.Abs(scale.y), 1e-5f),
                collisionRadius / Mathf.Max(Mathf.Abs(scale.z), 1e-5f));

            Vector3 min = roomBounds.center - half + margin;
            Vector3 max = roomBounds.center + half - margin;
            local = new Vector3(
                ClampAxis(local.x, min.x, max.x),
                ClampAxis(local.y, min.y, max.y),
                ClampAxis(local.z, min.z, max.z));

            return t.TransformPoint(local);
        }

        private static float ClampAxis(float value, float min, float max)
        {
            // Box thinner than the camera: stick to its center instead of flipping.
            return min > max ? (min + max) * 0.5f : Mathf.Clamp(value, min, max);
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            return angle > 180f ? angle - 360f : angle;
        }

        private static T Read<T>(InputActionReference reference) where T : struct
        {
            return reference != null && reference.action != null ? reference.action.ReadValue<T>() : default;
        }

        private void SetActionsEnabled(bool enabled)
        {
            foreach (InputActionReference reference in new[] { moveAction, lookAction, rotateAction, verticalAction, zoomAction })
            {
                if (reference == null || reference.action == null)
                    continue;

                if (enabled) reference.action.Enable();
                else reference.action.Disable();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, collisionRadius);

            if (roomBounds == null)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.matrix = roomBounds.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(roomBounds.center, roomBounds.size);
        }
    }
}
