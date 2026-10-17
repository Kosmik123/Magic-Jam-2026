using UnityEngine;

namespace MagicJam.Interaction
{
    /// <summary>
    /// Non-interactive movement target (doors, passages, floor marks).
    /// </summary>
    public class NavPoint : MonoBehaviour
    {
        [Tooltip("Where the actor walks to. Defaults to this transform.")]
        [SerializeField]
        private Transform destination;

        public Transform Destination => destination != null ? destination : transform;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(Destination.position, 0.25f);
        }
    }
}
