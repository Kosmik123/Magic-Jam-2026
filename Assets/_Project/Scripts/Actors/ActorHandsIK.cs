using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace MagicJam.Actors
{
    /// <summary>
    /// Moves the actor's hands with the TwoBoneIK constraints of its rig: places the constraint target at a
    /// point and blends the constraint weight in (reach) or out (release). Decides nothing.
    /// </summary>
    public class ActorHandsIK : MonoBehaviour
    {
        [SerializeField]
        private TwoBoneIKConstraint leftHand;

        [SerializeField]
        private TwoBoneIKConstraint rightHand;

        [Tooltip("Seconds from the animated pose to touching the point.")]
        [SerializeField]
        private float reachTime = 0.4f;

        [Tooltip("Seconds from touching the point back to the animated pose.")]
        [SerializeField]
        private float releaseTime = 0.3f;

        [SerializeField]
        private AnimationCurve blendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private void Awake()
        {
            ResetHands();
        }

        /// <summary>Snaps both hands back to the animated pose.</summary>
        public void ResetHands()
        {
            SetWeight(leftHand, 0f);
            SetWeight(rightHand, 0f);
        }

        /// <summary>Position of the hand bone, or of this object if the hand has no constraint.</summary>
        public Vector3 GetHandPosition(Hand hand)
        {
            TwoBoneIKConstraint constraint = Get(hand);
            if (constraint && constraint.data.tip)
                return constraint.data.tip.position;

            return transform.position;
        }

        /// <summary>
        /// Blends the hand onto the point. Without a rotation the hand keeps the rotation it has when the reach starts.
        /// </summary>
        public IEnumerator Reach(Hand hand, Vector3 position, Quaternion? rotation = null)
        {
            TwoBoneIKConstraint constraint = Get(hand);
            if (constraint == null || constraint.data.target == null)
                yield break;

            Quaternion targetRotation = rotation ?? GetTipRotation(constraint);
            yield return Blend(constraint, 1f, reachTime, position, targetRotation);
        }

        /// <summary>Blends the hand back to the animated pose.</summary>
        public IEnumerator Release(Hand hand)
        {
            TwoBoneIKConstraint constraint = Get(hand);
            if (constraint == null || constraint.data.target == null)
                yield break;

            Transform target = constraint.data.target;
            yield return Blend(constraint, 0f, releaseTime, target.position, target.rotation);
        }

        private IEnumerator Blend(TwoBoneIKConstraint constraint, float to, float duration, Vector3 position, Quaternion rotation)
        {
            Transform target = constraint.data.target;
            float from = constraint.weight;
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                target.SetPositionAndRotation(position, rotation);
                SetWeight(constraint, Mathf.Lerp(from, to, blendCurve.Evaluate(time / duration)));
                yield return null;
            }

            target.SetPositionAndRotation(position, rotation);
            SetWeight(constraint, to);
        }

        private TwoBoneIKConstraint Get(Hand hand)
        {
            return hand == Hand.Left ? leftHand : rightHand;
        }

        private static Quaternion GetTipRotation(TwoBoneIKConstraint constraint)
        {
            Transform tip = constraint.data.tip;
            return tip ? tip.rotation : constraint.data.target.rotation;
        }

        private static void SetWeight(TwoBoneIKConstraint constraint, float weight)
        {
            if (constraint)
                constraint.weight = weight;
        }
    }
}
