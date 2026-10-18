using UnityEngine;

namespace MagicJam.CameraSystem
{
    /// <summary>
    /// Marks an object the <see cref="OperatorCamera"/> is allowed to focus on.
    /// The camera can only focus on objects that have this component.
    /// </summary>
    public class FocusPoint : MonoBehaviour
    {
        [Tooltip("Offset from the object's pivot, in its local space (e.g. to aim at a face instead of the feet).")]
        [SerializeField]
        private Vector3 localOffset;

        public Vector3 Position => transform.TransformPoint(localOffset);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(Position, 0.1f);
        }
    }
}
