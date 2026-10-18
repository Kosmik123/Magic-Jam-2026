using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

namespace MagicJam.Actors
{
    /// <summary>
    /// Moves the actor along a spline over a fixed time with its NavMesh navigation off, e.g. getting out of bed
    /// while the interaction animation plays. The move starts where the actor is: the gap to the first knot
    /// fades out along the way. Navigation is turned back on at the end, so the last knot should lie on the NavMesh.
    /// </summary>
    public class SplinePathAction : InteractionAction
    {
        [SerializeField]
        private SplineContainer path;

        [Tooltip("Seconds the move takes; match it to the animation.")]
        [SerializeField]
        private float duration = 1.5f;

        [Tooltip("Progress along the path (0-1) over the normalized time.")]
        [SerializeField]
        private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Optional. Rotation the actor turns to over the move. Empty = keeps its rotation.")]
        [SerializeField]
        private Transform endRotation;

        [Tooltip("Turn the actor's NavMesh navigation on when the move ends.")]
        [SerializeField]
        private bool enableNavigationAtEnd = true;

        public override IEnumerator Perform(ActorInteractor actor)
        {
            if (path == null)
            {
                Debug.LogWarning($"{name} has no path.", this);
                yield break;
            }

            Transform body = actor.transform;
            ActorMovement movement = actor.Movement;
            if (movement)
                movement.DisableNavigation();

            Vector3 startOffset = body.position - Evaluate(0f);
            Quaternion startRotation = body.rotation;
            float time = 0f;
            while (time < duration)
            {
                Place(body, time / duration, startOffset, startRotation);
                yield return null;
                time += Time.deltaTime;
            }

            Place(body, 1f, startOffset, startRotation);
            if (enableNavigationAtEnd && movement)
                movement.EnableNavigation();
        }

        private void Place(Transform body, float normalizedTime, Vector3 startOffset, Quaternion startRotation)
        {
            float progress = easing.Evaluate(Mathf.Clamp01(normalizedTime));
            body.position = Evaluate(progress) + startOffset * (1f - progress);
            if (endRotation)
                body.rotation = Quaternion.Slerp(startRotation, endRotation.rotation, progress);
        }

        private Vector3 Evaluate(float t)
        {
            return path.EvaluatePosition(Mathf.Clamp01(t));
        }
    }
}
