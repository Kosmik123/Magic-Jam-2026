using System;
using MagicJam.CameraSystem.Shots;
using UnityEngine;
using UnityEngine.AI;

namespace MagicJam.Actors
{
    /// <summary>
    /// Picks a point on the NavMesh around the floor point the camera looks at: a ray goes from the frame
    /// center (horizontally) down to the floor, then a random point near the hit is sampled on the NavMesh.
    /// </summary>
    [Serializable]
    public class FrameWanderPoint
    {
        [Tooltip("Layers of the floor. Exclude the actors so the ray does not hit them.")]
        [SerializeField]
        private LayerMask floorMask = ~0;

        [SerializeField]
        private float maxRayDistance = 50f;

        [Tooltip("On: the ray starts half of the actor's frame height below the frame center (about its feet). Off or actor not in frame: the fallback height is used.")]
        [SerializeField]
        private bool useActorHeight = true;

        [Tooltip("Viewport height of the ray (0 = bottom of the frame, 1 = top).")]
        [Range(0f, 1f)]
        [SerializeField]
        private float fallbackViewportY = 1f / 3f;

        [Tooltip("Max distance of the random point from the floor hit.")]
        [SerializeField]
        private float randomRadius = 1f;

        [Tooltip("Max distance from the random point to the NavMesh.")]
        [SerializeField]
        private float navMeshSampleDistance = 1f;

        [SerializeField]
        private int attempts = 5;

        public bool TryGetPoint(ShotState state, Frameable actor, out Vector3 point)
        {
            point = default;
            Camera cam = state.Camera;
            if (cam == null)
                return false;

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, GetViewportY(state, actor), 0f));
            if (Physics.Raycast(ray, out RaycastHit floorHit, maxRayDistance, floorMask, QueryTriggerInteraction.Ignore) == false)
                return false;

            for (int i = 0; i < attempts; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * randomRadius;
                Vector3 candidate = floorHit.point + new Vector3(offset.x, 0f, offset.y);
                if (TrySample(candidate, out point))
                    return true;
            }

            return TrySample(floorHit.point, out point);
        }

        private float GetViewportY(ShotState state, Frameable actor)
        {
            if (useActorHeight && actor && state.TryGetView(actor, out FrameableView view))
                return Mathf.Clamp01(0.5f - view.HeightFraction * 0.5f);

            return fallbackViewportY;
        }

        private bool TrySample(Vector3 position, out Vector3 point)
        {
            bool found = NavMesh.SamplePosition(position, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas);
            point = hit.position;
            return found;
        }
    }
}
