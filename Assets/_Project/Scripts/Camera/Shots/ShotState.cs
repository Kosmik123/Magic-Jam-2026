using System.Collections.Generic;
using UnityEngine;

namespace MagicJam.CameraSystem.Shots
{
    /// <summary>
    /// What the camera sees right now. <see cref="ShotDetector"/> refreshes it before every detection,
    /// classifiers only read it (and report the subjects of the shot they recognized).
    /// </summary>
    public class ShotState : MonoBehaviour
    {
        [Tooltip("Camera the shots are detected for. Empty = the Camera on this object.")]
        [SerializeField]
        private Camera cam;

        [Tooltip("Layers that hide a point from the camera (walls, furniture). The subject's own colliders are ignored.")]
        [SerializeField]
        private LayerMask occlusionMask = ~0;

        [Tooltip("Border of the viewport that does not count as in frame, e.g. the letterbox bars.")]
        [Range(0f, 0.5f)]
        [SerializeField]
        private float viewportMargin = 0.02f;

        private readonly Plane[] frustumPlanes = new Plane[6];
        private readonly List<FrameableView> visible = new List<FrameableView>();
        private readonly List<Frameable> subjects = new List<Frameable>();

        public Camera Camera => cam;

        /// <summary>Frameables in the camera frustum, the one closest to the frame center first.</summary>
        public IReadOnlyList<FrameableView> Visible => visible;

        /// <summary>Objects that decided the current shot, the one closest to the frame center first.</summary>
        public IReadOnlyList<Frameable> Subjects => subjects;

        private void Awake()
        {
            if (cam == null)
                cam = GetComponent<Camera>();
        }

        public void Refresh()
        {
            visible.Clear();
            GeometryUtility.CalculateFrustumPlanes(cam, frustumPlanes);

            IReadOnlyList<Frameable> all = Frameable.All;
            for (int i = 0; i < all.Count; i++)
            {
                Bounds bounds = all[i].WorldBounds;
                if (GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
                    visible.Add(CreateView(all[i], bounds));
            }

            visible.Sort((a, b) => a.CenterDistance.CompareTo(b.CenterDistance));
        }

        public void ClearSubjects()
        {
            subjects.Clear();
        }

        public void AddSubject(Frameable subject)
        {
            subjects.Add(subject);
        }

        private FrameableView CreateView(Frameable frameable, Bounds bounds)
        {
            bool humanoid = frameable.Kind == FrameableKind.Humanoid;
            return new FrameableView(
                frameable,
                GetHeightFraction(bounds),
                GetCenterDistance(bounds),
                humanoid && IsVisible(frameable.Head, frameable),
                humanoid && IsVisible(frameable.Waist, frameable),
                humanoid && IsVisible(frameable.Knees, frameable),
                humanoid && IsVisible(frameable.Feet, frameable));
        }

        private float GetHeightFraction(Bounds bounds)
        {
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z);

                Vector3 viewport = cam.WorldToViewportPoint(corner);
                if (viewport.z <= 0f)
                    continue;

                minY = Mathf.Min(minY, viewport.y);
                maxY = Mathf.Max(maxY, viewport.y);
            }

            return maxY > minY ? maxY - minY : 0f;
        }

        private float GetCenterDistance(Bounds bounds)
        {
            Vector3 center = cam.WorldToViewportPoint(bounds.center);
            if (center.z <= 0f)
                return float.MaxValue;

            return new Vector2(center.x - 0.5f, center.y - 0.5f).magnitude;
        }

        private bool IsVisible(Transform point, Frameable owner)
        {
            if (point == null)
                return false;

            Vector3 viewport = cam.WorldToViewportPoint(point.position);
            if (viewport.z <= 0f)
                return false;

            float max = 1f - viewportMargin;
            if (viewport.x < viewportMargin || viewport.x > max || viewport.y < viewportMargin || viewport.y > max)
                return false;

            bool blocked = Physics.Linecast(
                cam.transform.position,
                point.position,
                out RaycastHit hit,
                occlusionMask,
                QueryTriggerInteraction.Ignore);

            return blocked == false || hit.transform.IsChildOf(owner.transform);
        }
    }
}
