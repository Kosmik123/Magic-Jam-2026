using System;
using System.Collections.Generic;
using MagicJam.Interaction;
using UnityEngine;
using UnityEngine.AI;

namespace MagicJam.Actors
{
    /// <summary>
    /// Actor state machine (GDD section 7). Knows nothing about cameras or shots – it only executes
    /// commands: <see cref="GoTo"/>, <see cref="Interact"/>, <see cref="FleeFrom"/>, plus the set modes
    /// <see cref="StartAction"/>, <see cref="Cut"/> and <see cref="ReturnToMark(Vector3, Quaternion)"/>.
    /// Fully deterministic – no randomness.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ActorController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Animator animator;

        [Tooltip("Where picked-up items are attached. Defaults to the actor root.")]
        [SerializeField]
        private Transform handSocket;

        [Header("Interaction")]
        [Tooltip("Max horizontal distance to an interaction point to act without walking first.")]
        [SerializeField]
        private float interactRange = 0.75f;

        [SerializeField]
        private float interactDuration = 1.5f;

        [SerializeField]
        private float failDuration = 2f;

        [Tooltip("Degrees per second when turning to face a target while standing.")]
        [SerializeField]
        private float turnSpeed = 360f;

        [Header("Flee")]
        [SerializeField]
        private float fleeDistance = 4f;

        [Header("Off duty (break)")]
        [SerializeField]
        private OffDutyStep[] offDutySteps = Array.Empty<OffDutyStep>();

        [Header("Animator parameters (leave empty to skip)")]
        [SerializeField]
        private string speedParam = "Speed";

        [SerializeField]
        private string pickupTrigger = "PickUp";

        [SerializeField]
        private string useTrigger = "Use";

        [SerializeField]
        private string talkTrigger = "Talk";

        [SerializeField]
        private string physicalTrigger = "Physical";

        [SerializeField]
        private string failTrigger = "Fail";

        private NavMeshAgent agent;
        private readonly HashSet<string> animatorParams = new HashSet<string>();

        private ActorState state = ActorState.Idle;
        private bool inAction;
        private float stateTimer;

        // Current interaction (approached in MoveTo, performed in Interact).
        private Interactable targetInteractable;

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
        /// <summary>True when the actor can accept GoTo / Interact (not mid-interaction or failing).</summary>
        public bool IsBusy => state == ActorState.Interact || state == ActorState.Fail;
        /// <summary>True when the actor stands on its mark after a retake (or has not moved since the take began).</summary>
        public bool IsReady => ready;
        public Interactable HeldItem => heldItem;
        public Vector3 MarkPosition => markPosition;
        public Quaternion MarkRotation => markRotation;

        public event Action<ActorState> StateChanged;
        /// <summary>Raised after an interaction finishes successfully (item picked, used, talked, physical action done).</summary>
        public event Action<ActorController, Interactable> InteractionCompleted;
        /// <summary>Raised when the actor enters Fail – the take is ruined.</summary>
        public event Action<ActorController> Failed;
        /// <summary>Raised when the actor reaches its mark after ReturnToMark.</summary>
        public event Action<ActorController> ReadyOnMark;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (handSocket == null)
                handSocket = transform;

            if (animator != null)
            {
                foreach (var param in animator.parameters)
                {
                    animatorParams.Add(param.name);
                }
            }

            CaptureMark();
        }

        private void Update()
        {
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

        #region Public API – commands

        /// <summary>Walks to the given position on the NavMesh. Returns false if busy or unreachable.</summary>
        public bool GoTo(Vector3 destination)
        {
            if (IsBusy)
                return false;

            ClearTarget();
            if (!SetDestination(destination))
                return false;

            ready = false;
            ChangeState(ActorState.MoveTo);
            return true;
        }

        /// <summary>
        /// Walks up to the interactable (if not already in range) and performs the interaction matching its type.
        /// Using a Usable without the required item ends in Fail. Returns false if busy or not interactable.
        /// </summary>
        public bool Interact(Interactable interactable)
        {
            if (IsBusy || interactable == null || interactable.Type == InteractableType.Threat)
                return false;
            if (interactable == heldItem)
                return false;

            ready = false;
            targetInteractable = interactable;

            if (IsWithinInteractRange(interactable))
            {
                BeginInteract();
                return true;
            }

            if (!SetDestination(interactable.InteractionPoint.position))
            {
                ClearTarget();
                return false;
            }
            ChangeState(ActorState.MoveTo);
            return true;
        }

        /// <summary>Runs <see cref="fleeDistance"/> away from the threat position. Interrupts everything except Fail.</summary>
        public bool FleeFrom(Vector3 threatPosition)
        {
            if (state == ActorState.Fail)
                return false;

            Vector3 away = transform.position - threatPosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
                away = -transform.forward;

            Vector3 target = transform.position + away.normalized * fleeDistance;
            if (!NavMesh.SamplePosition(target, out var hit, fleeDistance, agent.areaMask) || !SetDestination(hit.position))
                return false;

            ClearTarget();
            ready = false;
            ChangeState(ActorState.Flee);
            return true;
        }

        /// <summary>Stops whatever the actor is doing and stands still.</summary>
        public void Stop()
        {
            ClearTarget();
            StopAgent();
            ChangeState(ActorState.Idle);
        }

        #endregion

        #region Public API – set modes

        /// <summary>Action! Resumes from the cut pose (continuity) or, during ReturnToMark, stops where the actor is (false start).</summary>
        public void StartAction()
        {
            inAction = true;

            if (state == ActorState.ReturnToMark)
            {
                // False start: the actor plays from wherever it currently is.
                StopAgent();
                ready = false;
            }
            else if (hasCutPose)
            {
                agent.Warp(cutPosition);
                transform.rotation = cutRotation;
            }

            hasCutPose = false;
            ClearTarget();
            StopAgent();
            ChangeState(ActorState.Idle);
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

        /// <summary>Retake: drops the held item (SnapshotSystem restores props) and walks back to the captured mark.</summary>
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
            if (targetInteractable.Type == InteractableType.Usable &&
                (heldItem == null || !targetInteractable.Accepts(heldItem)))
            {
                EnterFail();
                return;
            }

            stateTimer = interactDuration;
            ChangeState(ActorState.Interact);
            SetAnimatorTrigger(TriggerFor(targetInteractable.Type));
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
                InteractionCompleted?.Invoke(this, interactable);
            }

            ChangeState(ActorState.Idle);
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
            offDutyWalking = step.Spot != null && SetDestination(step.Spot.position);
            if (!offDutyWalking)
                StartOffDutyAnimation(step);
        }

        private void StartOffDutyAnimation(OffDutyStep step)
        {
            offDutyWalking = false;
            stateTimer = step.Duration;
            if (step.Spot != null)
                transform.rotation = Quaternion.Euler(0f, step.Spot.eulerAngles.y, 0f);
            SetAnimatorTrigger(step.AnimTrigger);
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

        private bool IsWithinInteractRange(Interactable interactable)
        {
            Vector3 delta = interactable.InteractionPoint.position - transform.position;
            delta.y = 0f;
            return delta.magnitude <= interactRange;
        }

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

        private string TriggerFor(InteractableType type)
        {
			return type switch
			{
				InteractableType.Pickup => pickupTrigger,
				InteractableType.Usable => useTrigger,
				InteractableType.Talkable => talkTrigger,
				InteractableType.Physical => physicalTrigger,
				_ => null,
			};
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
