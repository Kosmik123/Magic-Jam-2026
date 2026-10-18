using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MagicJam.CameraSystem
{
    /// <summary>
    /// Free-moving film camera operated by the player.
    /// Input is cached in Update and applied in LateUpdate.
    /// W/S move along the camera's forward direction (strictly horizontal while Shift is held), A/D strafe, mouse rotates while the rotate button is held, Q/E lowers/raises, scroll zooms (changes FOV).
    /// Movement is clamped to a room bounds box and blocked by colliders on the collision mask.
    /// With a focus set, the camera first turns to face it and only then moves along its own axes as usual,
    /// so W/S approach/retreat and A/D strafe around the focus; rotating with the mouse clears the focus.
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

        [Tooltip("While held, W/S move strictly horizontally; otherwise they follow the camera's forward direction.")]
        [SerializeField]
        private InputActionReference forceHorizontalAction;

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

        [Header("Focus")]
        [Tooltip("Optional. While set, the camera turns to face this focus point before every move, so W/S approach/retreat and A/D strafe around it. Cleared when the camera is rotated with the mouse.")]
        [SerializeField]
        private FocusPoint focus;

        [Tooltip("Closest the camera can get to the focus (horizontal distance).")]
        [SerializeField]
        private float minFocusDistance = 0.5f;

        [Tooltip("Time to turn toward a newly set focus (when SetFocus is called with smooth = true).")]
        [SerializeField]
        private float focusTurnSmoothTime = 0.25f;

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
        private const float TurnDoneAngle = 0.1f;

        private Camera cam;

        [ShowNonSerializedField]
        private float yaw;

        [ShowNonSerializedField]
        private float pitch;

        [ShowNonSerializedField]
        private Vector3 velocity;

        private Vector3 velocitySmoothing;

        [ShowNonSerializedField]
        private float targetFov;

        private float fovSmoothing;

        [ShowNonSerializedField]
        private bool isRotating;

        [ShowNonSerializedField]
        private bool isTurningToFocus;

        private float yawSmoothing;
        private float pitchSmoothing;

        // Input is read once per frame in Update and applied in LateUpdate.
        [ShowNonSerializedField]
        private Vector2 moveInput;

        [ShowNonSerializedField]
        private Vector2 lookInput;

        [ShowNonSerializedField]
        private float verticalInput;

        [ShowNonSerializedField]
        private float zoomInput;

        [ShowNonSerializedField]
        private bool rotateInput;

        [ShowNonSerializedField]
        private bool forceHorizontalInput;

        public Camera Camera => cam;

        /// <summary>Focus point the camera is facing, or null in free mode.</summary>
        public FocusPoint Focus => focus;

        /// <summary>Current movement velocity in world space.</summary>
        public Vector3 Velocity => velocity;

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
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            verticalInput = 0f;
            zoomInput = 0f;
            rotateInput = false;
            forceHorizontalInput = false;
        }

        private void Update()
        {
            CacheInput();
            SetRotating(rotateInput);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;

            UpdateRotation(isRotating ? lookInput : Vector2.zero);

            // Face the focus first, so the movement below is relative to the direction to it.
            if (focus != null)
                AimAtFocus(dt);

            UpdateZoom(zoomInput, dt);
            UpdateMovement(moveInput, verticalInput, dt);

            // Keep the focus centered after the move. While still turning to a new focus, the turn above leads.
            if (focus != null && isTurningToFocus == false)
                AimAtFocus(dt);
        }

        /// <summary>
        /// Makes the camera face the target and move relative to it. Passing null returns to free mode.
        /// </summary>
        /// <param name="target">Focus point to look at.</param>
        /// <param name="smooth">True to turn toward the target over focusTurnSmoothTime, false to snap instantly.</param>
        public void SetFocus(FocusPoint target, bool smooth = true)
        {
            if (target == null)
            {
                ClearFocus();
                return;
            }

            focus = target;
            isTurningToFocus = smooth;
            yawSmoothing = 0f;
            pitchSmoothing = 0f;
            if (smooth == false)
                AimAtFocus(0f);
        }

        public void ClearFocus()
        {
            focus = null;
            isTurningToFocus = false;
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

        private void CacheInput()
        {
            bool enabled = InputEnabled;
            moveInput = enabled ? Read<Vector2>(moveAction) : Vector2.zero;
            lookInput = enabled ? Read<Vector2>(lookAction) : Vector2.zero;
            verticalInput = enabled ? Read<float>(verticalAction) : 0f;
            zoomInput = enabled ? Read<float>(zoomAction) : 0f;
            rotateInput = enabled && Read<float>(rotateAction) > 0f;
            forceHorizontalInput = enabled && Read<float>(forceHorizontalAction) > 0f;
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
            // Rotating by hand takes the camera out of focus mode; it keeps its current orientation.
            if (focus != null && look != Vector2.zero)
                ClearFocus();

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
            // Sideways movement and forced-horizontal forward movement follow yaw only, so looking up/down
            // doesn't change height. Otherwise forward/back follows where the camera is looking.
            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 forward = forceHorizontalInput
                ? yawRotation * Vector3.forward
                : Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
            Vector3 right = yawRotation * Vector3.right;
            Vector3 targetVelocity = (right * move.x + forward * move.y) * moveSpeed
                                     + Vector3.up * (vertical * verticalSpeed);

            velocity = moveSmoothTime > 0f
                ? Vector3.SmoothDamp(velocity, targetVelocity, ref velocitySmoothing, moveSmoothTime, Mathf.Infinity, dt)
                : targetVelocity;

            Vector3 delta = velocity * dt;
            if (delta.sqrMagnitude < 1e-8f)
                return;

            if (focus != null)
                delta = LimitFocusApproach(delta);

            Vector3 position = MoveWithCollision(transform.position, delta);
            Vector3 clamped = ClampToBounds(position);

            // Kill velocity along axes blocked by bounds so the camera doesn't "push" against them.
            if (clamped != position)
                velocity = Vector3.ProjectOnPlane(velocity, (position - clamped).normalized);

            transform.position = clamped;
        }

        /// <summary>Stops the camera from getting closer to the focus than minFocusDistance (horizontally).</summary>
        private Vector3 LimitFocusApproach(Vector3 delta)
        {
            Vector3 current = transform.position;
            Vector3 next = current + delta;
            Vector3 flat = new Vector3(next.x - focus.Position.x, 0f, next.z - focus.Position.z);
            float distance = flat.magnitude;
            Vector3 currentFlat = new Vector3(current.x - focus.Position.x, 0f, current.z - focus.Position.z);

            // Already closer than the minimum: don't push the camera out, just don't allow getting closer.
            float limit = Mathf.Min(minFocusDistance, currentFlat.magnitude);
            if (distance >= limit)
                return delta;

            Vector3 direction = distance > 1e-4f ? flat / distance : currentFlat.normalized;
            next.x = focus.Position.x + direction.x * limit;
            next.z = focus.Position.z + direction.z * limit;
            return next - current;
        }

        private void AimAtFocus(float dt)
        {
            Vector3 direction = focus.Position - transform.position;
            if (direction.sqrMagnitude < 1e-6f)
                return;

            Vector3 euler = Quaternion.LookRotation(direction).eulerAngles;
            float targetYaw = euler.y;
            float targetPitch = Mathf.Clamp(NormalizeAngle(euler.x), minPitch, maxPitch);

            if (isTurningToFocus)
            {
                yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawSmoothing, focusTurnSmoothTime, Mathf.Infinity, dt);
                pitch = Mathf.Clamp(
                    Mathf.SmoothDampAngle(pitch, targetPitch, ref pitchSmoothing, focusTurnSmoothTime, Mathf.Infinity, dt),
                    minPitch,
                    maxPitch);

                if (Mathf.Abs(Mathf.DeltaAngle(yaw, targetYaw)) < TurnDoneAngle
                    && Mathf.Abs(Mathf.DeltaAngle(pitch, targetPitch)) < TurnDoneAngle)
                {
                    isTurningToFocus = false;
                }
            }
            else
            {
                yaw = targetYaw;
                pitch = targetPitch;
            }

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
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
                if (Physics.SphereCast(from, collisionRadius, direction, out RaycastHit hit, distance + Skin,
                        collisionMask, QueryTriggerInteraction.Ignore) == false)
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
            foreach (InputActionReference reference in new[] { moveAction, lookAction, rotateAction, forceHorizontalAction, verticalAction, zoomAction })
            {
                if (reference == null || reference.action == null)
                    continue;

                if (enabled)
                    reference.action.Enable();
                else
                    reference.action.Disable();
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
