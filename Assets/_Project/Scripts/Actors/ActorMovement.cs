using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AI;

namespace MagicJam.Actors
{
    /// <summary>Walks the actor over the NavMesh. Only executes commands, decides nothing.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ActorMovement : MonoBehaviour
    {
        private const float ArriveTolerance = 0.05f;

        /// <summary>Raised when the actor reached the destination.</summary>
        public event Action Arrived;

        /// <summary>
        /// Raised when there is no full path to the destination. The actor still walks as close as it can.
        /// </summary>
        public event Action DestinationUnreachable;

        [Tooltip("Max distance from the requested position to the NavMesh.")]
        [SerializeField]
        private float navMeshSampleDistance = 1.5f;

        [Tooltip("Optional. Gets the walking speed (m/s) in the float parameter below.")]
        [SerializeField]
        private Animator animator;

        [SerializeField]
        private string speedParameter = "Speed";

        private NavMeshAgent agent;
        private int speedHash;
        private bool hasSpeedParameter;
        private bool pathChecked;

        [Header("States")]
        [ShowNonSerializedField]
        private bool isMoving;

        public bool IsMoving => isMoving;

        public float Radius => agent.radius;

        public Vector3 Destination => agent.destination;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            speedHash = Animator.StringToHash(speedParameter);
            hasSpeedParameter = animator && HasFloatParameter(animator, speedHash);
        }

        private void Update()
        {
            if (hasSpeedParameter)
                animator.SetFloat(speedHash, agent.velocity.magnitude);

            if (isMoving == false || agent.pathPending)
                return;

            if (pathChecked == false)
            {
                pathChecked = true;
                if (agent.pathStatus != NavMeshPathStatus.PathComplete)
                    DestinationUnreachable?.Invoke();
            }

            if (agent.remainingDistance > agent.stoppingDistance + ArriveTolerance)
                return;

            isMoving = false;
            Arrived?.Invoke();
        }

        /// <summary>
        /// Walks to the NavMesh point closest to the position and stops within the stopping distance.
        /// Returns false if there is no NavMesh near the position.
        /// </summary>
        public bool GoTo(Vector3 position, float stoppingDistance = 0.1f)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas) == false)
                return false;

            agent.stoppingDistance = stoppingDistance;
            if (agent.SetDestination(hit.position) == false)
                return false;

            isMoving = true;
            pathChecked = false;
            return true;
        }

        public void Stop()
        {
            if (agent.isOnNavMesh)
                agent.ResetPath();

            isMoving = false;
        }

        /// <summary>Turns the actor in place to face the position (horizontally).</summary>
        public void Face(Vector3 position)
        {
            Vector3 direction = position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction);
        }

        private static bool HasFloatParameter(Animator animator, int hash)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].nameHash == hash && parameters[i].type == AnimatorControllerParameterType.Float)
                    return true;
            }

            return false;
        }
    }
}
