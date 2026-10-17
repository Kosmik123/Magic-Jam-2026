using System;
using System.Collections.Generic;
using MagicJam.CameraSystem;
using MagicJam.Interaction;
using UnityEngine;
using UnityEngine.AI;

namespace MagicJam.Actors
{
    public enum ActorState
    {
        /// <summary>Standing still (on a mark or after finishing an action).</summary>
        Idle,
        /// <summary>Walking with the NavMeshAgent to a target (wide shot or approaching an interactable).</summary>
        MoveTo,
        /// <summary>Performing the shot-dependent action on the focused object.</summary>
        Interact,
        /// <summary>Running away from a threat in focus. Highest priority.</summary>
        Flee,
        /// <summary>Break mode: deterministic list of off-duty behaviours.</summary>
        OffDuty,
        /// <summary>After a retake: walking back to the starting mark of the take.</summary>
        ReturnToMark,
        /// <summary>Funny failure, the take is ruined.</summary>
        Fail
    }

    /// <summary>
    /// One off-duty behaviour: walk to a spot, play an animation for a while (coffee, gossip, catering...).
    /// </summary>
    [Serializable]
    public class OffDutyStep
    {
        public Transform spot;
        [Tooltip("Animator trigger played on arrival. Optional.")]
        public string animTrigger;
        public float duration = 5f;
    }

    /// <summary>
    /// Actor state machine (GDD section 7). The player never controls the actor directly:
    /// ShotDetector reports the current shot and focused object via <see cref="SetShot"/>, and once that
    /// framing is stable for <see cref="shotStableTime"/> the actor performs the matching action.
    /// ModeController drives <see cref="StartAction"/>, <see cref="Cut"/> and <see cref="Retake"/>.
    /// Fully deterministic – no randomness.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ActorController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [Tooltip("Where picked-up items are attached. Defaults to the actor root.")]
        [SerializeField] private Transform handSocket;

        [Header("Shot response")]
        [Tooltip("Shot and focus must stay unchanged this long before the actor reacts.")]
        [SerializeField] private float shotStableTime = 0.5f;
        [Tooltip("Max horizontal distance to an interaction point to act without walking first.")]
        [SerializeField] private float interactRange = 0.75f;
        [SerializeField] private float interactDuration = 1.5f;
        [SerializeField] private float failDuration = 2f;
        [Tooltip("Degrees per second when turning to face a target while standing.")]
        [SerializeField] private float turnSpeed = 360f;

        [Header("Flee")]
        [SerializeField] private float fleeDistance = 4f;

        [Header("Off duty (break)")]
        [SerializeField] private OffDutyStep[] offDutySteps = Array.Empty<OffDutyStep>();

        [Header("Animator parameters (leave empty to skip)")]
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string pickupTrigger = "PickUp";
        [SerializeField] private string useTrigger = "Use";
        [SerializeField] private string talkTrigger = "Talk";
        [SerializeField] private string physicalTrigger = "Physical";
        [SerializeField] private string failTrigger = "Fail";

        private NavMeshAgent agent;
        private readonly HashSet<string> animatorParams = new HashSet<string>();

        private ActorState state = ActorState.Idle;
        private bool inAction;
        private float stateTimer;

        // Shot stability tracking.
        private ShotType pendingShot;
        private Transform pendingFocus;
        private float pendingStableTime;
        private bool pendingHandled;

        // Current interaction (approached in MoveTo, performed in Interact).
        private Interactable targetInteractable;
        private ShotType targetShot;

        // Starting mark of the take (for retakes).
        private Vector3 markPosition;
        private Quaternion markRotation;

        // Pose at the moment of the cut (continuity when action resumes).
        private bool hasCutPose;
        private Vector3 cutPosition;
        private Quaternion cutRotation;

        private int offDutyIndex;
        private bool offDutyWalking;

        private Interactable heldItem;
        private Transform heldItemOriginalParent;
        private bool ready = true;

        public ActorState State => state;
        public bool IsInAction => inAction;
        /// <summary>True when the actor stands on its mark after a retake (or has not moved since the take began).</summary>
        public bool IsReady => ready;
        public Interactable HeldItem => heldItem;
        public Vector3 MarkPosition => markPosition;
        public Quaternion MarkRotation => markRotation;

        public event Action<ActorState> StateChanged;
        /// <summary>Raised after an interaction finishes successfully (item picked, used, talked, physical action done).</summary>
        public event Action<ActorController, Interactable, ShotType> InteractionCompleted;
        /// <summary>Raised when the actor enters Fail – the take is ruined.</summary>
        public event Action<ActorController> Failed;
        /// <summary>Raised when the actor reaches its mark after a retake.</summary>
        public event Action<ActorController> ReadyOnMark;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (handSocket == null)
                handSocket = transform;

            if (animator != null)
            {
                foreach (var param in animator.parameters)
                    animatorParams.Add(param.name);
            }

            CaptureMark();
        }

        private void Update()
        {
            if (inAction)
                UpdateShotStability();

            switch (state)
            {
                case ActorState.MoveTo: UpdateMoveTo(); break;
                case ActorState.Interact: UpdateInteract(); break;
                case ActorState.Flee: UpdateFlee(); break;
                case ActorState.OffDuty: UpdateOffDuty(); break;
                case ActorState.ReturnToMark: UpdateReturnToMark(); break;
                case ActorState.Fail: UpdateFail(); break;
            }

            SetAnimatorFloat(speedParam, agent.velocity.magnitude);
        }

        #region Public API – shot input

        /// <summary>
        /// Reports the current framing. Safe to call every frame; the actor reacts once the same
        /// shot + focus has been held for <see cref="shotStableTime"/>.
        /// </summary>
        public void SetShot(ShotType shot, Transform focus)
        {
            if (shot == pendingShot && focus == pendingFocus)
                return;

            pendingShot = shot;
            pendingFocus = focus;
            pendingStableTime = 0f;
            pendingHandled = false;
        }

        #endregion

        #region Public API – modes

        /// <summary>Action! Resumes from the cut pose (continuity) or, during ReturnToMark, starts where the actor is (false start).</summary>
        public void StartAction()
        {
            inAction = true;

            if (state == ActorState.ReturnToMark)
            {
                // False start: the actor plays from wherever it currently is.
                StopAgent();
            }
            else if (hasCutPose)
            {
                agent.Warp(cutPosition);
                transform.rotation = cutRotation;
            }

            hasCutPose = false;
            ready = false;
            ClearTarget();
            ChangeState(ActorState.Idle);

            // Re-evaluate the current framing from scratch.
            pendingStableTime = 0f;
            pendingHandled = false;
        }

        /// <summary>Cut! Remembers the pose for continuity and goes off duty.</summary>
        public void Cut()
        {
            if (!inAction)
                return;

            inAction = false;
            hasCutPose = true;
            cutPosition = transform.position;
            cutRotation = transform.rotation;

            ClearTarget();
            StopAgent();
            EnterOffDuty();
        }

        /// <summary>Retake: drops the held item (SnapshotSystem restores props) and walks back to the take's mark.</summary>
        public void Retake()
        {
            ReleaseHeldItem();
            ReturnToMark(markPosition, markRotation);
        }

        /// <summary>Walks back to the given mark on the NavMesh (no teleport), then takes the start pose.</summary>
        public void ReturnToMark(Vector3 position, Quaternion rotation)
        {
            inAction = false;
            hasCutPose = false;
            ready = false;
            markPosition = position;
            markRotation = rotation;

            ClearTarget();
            if (!SetDestination(position))
            {
                agent.Warp(position);
                ArriveOnMark();
                return;
            }
            ChangeState(ActorState.ReturnToMark);
        }

        /// <summary>Stores the current pose as the starting mark of the take.</summary>
        public void CaptureMark()
        {
            markPosition = transform.position;
            markRotation = transform.rotation;
        }

        /// <summary>Detaches the held item and restores its colliders/physics. Leaves it where it is.</summary>
        public void ReleaseHeldItem()
        {
            if (heldItem == null)
                return;

            heldItem.transform.SetParent(heldItemOriginalParent, true);
            SetItemPhysics(heldItem, true);
            heldItem = null;
            heldItemOriginalParent = null;
        }

        #endregion

        #region Shot evaluation

        private void UpdateShotStability()
        {
            if (pendingHandled)
                return;

            pendingStableTime += Time.deltaTime;
            if (pendingStableTime < shotStableTime)
                return;

            if (TryHandleShot(pendingShot, pendingFocus))
                pendingHandled = true;
        }

        /// <summary>Returns false when the actor is busy and the shot should be retried later.</summary>
        private bool TryHandleShot(ShotType shot, Transform focus)
        {
            if (focus == null || shot == ShotType.None)
                return true;

            // Priority 1: flee from a threat in focus, whatever the shot.
            var threat = FindInteractable(focus, InteractableType.Threat);
            if (threat != null)
            {
                if (state == ActorState.Fail)
                    return false;
                StartFlee(threat.transform.position);
                return true;
            }

            if (state == ActorState.Interact || state == ActorState.Fail || state == ActorState.Flee)
                return false;

            // Priority 2: the shot's action on the focused object.
            switch (shot)
            {
                case ShotType.Wide:
                    StartWalk(focus);
                    break;
                case ShotType.Full:
                    Approach(FindInteractable(focus, InteractableType.Physical), shot);
                    break;
                case ShotType.American:
                    Approach(FindInteractable(focus, InteractableType.Usable), shot);
                    break;
                case ShotType.Medium:
                    Approach(FindInteractable(focus, InteractableType.Talkable), shot);
                    break;
                case ShotType.Detail:
                    Approach(FindInteractable(focus, InteractableType.Pickup), shot);
                    break;
            }
            return true;
        }

        /// <summary>An object may carry several Interactables (e.g. roommate: Talkable + Usable); pick the one matching the type.</summary>
        private static Interactable FindInteractable(Transform focus, InteractableType type)
        {
            foreach (var interactable in focus.GetComponentsInParent<Interactable>())
            {
                if (interactable.isActiveAndEnabled && interactable.Type == type)
                    return interactable;
            }
            return null;
        }

        private void StartWalk(Transform focus)
        {
            Vector3 destination;
            var navPoint = focus.GetComponentInParent<NavPoint>();
            if (navPoint != null)
            {
                destination = navPoint.Destination.position;
            }
            else
            {
                var interactable = focus.GetComponentInParent<Interactable>();
                destination = interactable != null ? interactable.InteractionPoint.position : focus.position;
            }

            ClearTarget();
            if (SetDestination(destination))
                ChangeState(ActorState.MoveTo);
        }

        private void Approach(Interactable interactable, ShotType shot)
        {
            if (interactable == null)
                return;
            if (shot == ShotType.Detail && interactable == heldItem)
                return;

            targetInteractable = interactable;
            targetShot = shot;

            if (IsWithinInteractRange(interactable))
            {
                BeginInteract();
                return;
            }

            if (SetDestination(interactable.InteractionPoint.position))
                ChangeState(ActorState.MoveTo);
            else
                ClearTarget();
        }

        private bool IsWithinInteractRange(Interactable interactable)
        {
            Vector3 delta = interactable.InteractionPoint.position - transform.position;
            delta.y = 0f;
            return delta.magnitude <= interactRange;
        }

        #endregion

        #region States

        private void UpdateMoveTo()
        {
            if (!HasArrived())
                return;

            if (targetInteractable != null)
                BeginInteract();
            else
                ChangeState(ActorState.Idle);
        }

        private void BeginInteract()
        {
            StopAgent();

            // Using an item: wrong or missing item is a funny failure.
            if (targetShot == ShotType.American &&
                (heldItem == null || !targetInteractable.Accepts(heldItem)))
            {
                EnterFail();
                return;
            }

            stateTimer = interactDuration;
            ChangeState(ActorState.Interact);
            SetAnimatorTrigger(TriggerFor(targetShot));
        }

        private void UpdateInteract()
        {
            if (targetInteractable != null)
                FaceTowards(targetInteractable.transform.position);

            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f)
                return;

            CompleteInteract();
        }

        private void CompleteInteract()
        {
            var interactable = targetInteractable;
            var shot = targetShot;
            ClearTarget();

            if (interactable != null)
            {
                switch (interactable.Type)
                {
                    case InteractableType.Pickup:
                        PickUp(interactable);
                        break;
                    case InteractableType.Usable:
                        if (interactable.ConsumeItem && heldItem != null)
                        {
                            var used = heldItem;
                            ReleaseHeldItem();
                            used.gameObject.SetActive(false);
                        }
                        break;
                }

                interactable.NotifyInteracted();
                InteractionCompleted?.Invoke(this, interactable, shot);
            }

            ChangeState(ActorState.Idle);
        }

        private void StartFlee(Vector3 threatPosition)
        {
            Vector3 away = transform.position - threatPosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
                away = -transform.forward;

            ClearTarget();
            Vector3 target = transform.position + away.normalized * fleeDistance;
            if (NavMesh.SamplePosition(target, out var hit, fleeDistance, agent.areaMask) && SetDestination(hit.position))
                ChangeState(ActorState.Flee);
        }

        private void UpdateFlee()
        {
            if (HasArrived())
                ChangeState(ActorState.Idle);
        }

        private void EnterFail()
        {
            ClearTarget();
            StopAgent();
            stateTimer = failDuration;
            ChangeState(ActorState.Fail);
            SetAnimatorTrigger(failTrigger);
            Failed?.Invoke(this);
        }

        private void UpdateFail()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
                ChangeState(ActorState.Idle);
        }

        private void EnterOffDuty()
        {
            if (offDutySteps == null || offDutySteps.Length == 0)
            {
                ChangeState(ActorState.Idle);
                return;
            }

            // Always start from the first step so every break plays out the same way.
            offDutyIndex = 0;
            ChangeState(ActorState.OffDuty);
            BeginOffDutyStep();
        }

        private void BeginOffDutyStep()
        {
            var step = offDutySteps[offDutyIndex];
            offDutyWalking = step.spot != null && SetDestination(step.spot.position);
            if (!offDutyWalking)
                StartOffDutyAnimation(step);
        }

        private void StartOffDutyAnimation(OffDutyStep step)
        {
            offDutyWalking = false;
            stateTimer = step.duration;
            if (step.spot != null)
                transform.rotation = Quaternion.Euler(0f, step.spot.eulerAngles.y, 0f);
            SetAnimatorTrigger(step.animTrigger);
        }

        private void UpdateOffDuty()
        {
            var step = offDutySteps[offDutyIndex];
            if (offDutyWalking)
            {
                if (HasArrived())
                    StartOffDutyAnimation(step);
                return;
            }

            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f)
                return;

            offDutyIndex = (offDutyIndex + 1) % offDutySteps.Length;
            BeginOffDutyStep();
        }

        private void UpdateReturnToMark()
        {
            if (HasArrived())
                ArriveOnMark();
        }

        private void ArriveOnMark()
        {
            StopAgent();
            transform.rotation = markRotation;
            ready = true;
            ChangeState(ActorState.Idle);
            ReadyOnMark?.Invoke(this);
        }

        private void ChangeState(ActorState newState)
        {
            if (state == newState)
                return;
            state = newState;
            StateChanged?.Invoke(state);
        }

        #endregion

        #region Helpers

        private void PickUp(Interactable item)
        {
            if (heldItem != null)
                ReleaseHeldItem();

            heldItem = item;
            heldItemOriginalParent = item.transform.parent;
            SetItemPhysics(item, false);
            item.transform.SetParent(handSocket, false);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
        }

        private static void SetItemPhysics(Interactable item, bool enabled)
        {
            foreach (var col in item.GetComponentsInChildren<Collider>())
                col.enabled = enabled;
            if (item.TryGetComponent<Rigidbody>(out var body))
                body.isKinematic = !enabled;
        }

        private bool SetDestination(Vector3 destination)
        {
            if (!agent.isOnNavMesh)
                return false;
            agent.isStopped = false;
            return agent.SetDestination(destination);
        }

        private void StopAgent()
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        private bool HasArrived()
        {
            if (agent.pathPending)
                return false;
            if (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathInvalid)
                return true;
            return agent.remainingDistance <= agent.stoppingDistance + 0.05f;
        }

        private void ClearTarget()
        {
            targetInteractable = null;
            targetShot = ShotType.None;
        }

        private void FaceTowards(Vector3 point)
        {
            Vector3 dir = point - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                return;
            var target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        private string TriggerFor(ShotType shot)
        {
            switch (shot)
            {
                case ShotType.Detail: return pickupTrigger;
                case ShotType.American: return useTrigger;
                case ShotType.Medium: return talkTrigger;
                case ShotType.Full: return physicalTrigger;
                default: return null;
            }
        }

        private void SetAnimatorTrigger(string param)
        {
            if (animator != null && !string.IsNullOrEmpty(param) && animatorParams.Contains(param))
                animator.SetTrigger(param);
        }

        private void SetAnimatorFloat(string param, float value)
        {
            if (animator != null && !string.IsNullOrEmpty(param) && animatorParams.Contains(param))
                animator.SetFloat(param, value);
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Vector3 mark = Application.isPlaying ? markPosition : transform.position;
            Gizmos.DrawWireSphere(mark, 0.3f);

            if (targetInteractable != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, targetInteractable.InteractionPoint.position);
            }
        }
    }
}
