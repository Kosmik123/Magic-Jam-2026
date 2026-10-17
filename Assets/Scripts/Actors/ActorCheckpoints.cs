using UnityEngine;

namespace MagicJam.Actors
{
    /// <summary>
    /// Body reference points used by ShotDetector to decide the shot size (GDD section 6).
    /// Place the transforms on the character rig (or as plain children for a capsule placeholder).
    /// </summary>
    public class ActorCheckpoints : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private Transform waist;
        [SerializeField] private Transform knees;
        [SerializeField] private Transform feet;

        public Transform Head => head;
        public Transform Waist => waist;
        public Transform Knees => knees;
        public Transform Feet => feet;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            DrawPoint(head);
            DrawPoint(waist);
            DrawPoint(knees);
            DrawPoint(feet);
        }

        private static void DrawPoint(Transform point)
        {
            if (point != null)
                Gizmos.DrawWireSphere(point.position, 0.05f);
        }
    }
}
