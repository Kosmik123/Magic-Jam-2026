using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicJam.CameraSystem.Shots
{
    /// <summary>
    /// Close-up on an object: a visible prop near the frame center that is tall enough in the frame.
    /// </summary>
    [Serializable]
    public class ObjectCloseUpShotClassifier : ShotClassifier
    {
        [Tooltip("Height of the prop as a fraction of the frame height from which the shot is a close-up.")]
        [SerializeField]
        private float minHeight = 0.5f;

        [Tooltip("Max distance of the prop's center from the frame center (viewport units, 0.5 = frame edge).")]
        [SerializeField]
        private float maxCenterDistance = 0.3f;

        [Tooltip("On: any other visible prop in frame cancels the close-up.")]
        [SerializeField]
        private bool requireSingleProp = true;

        public override bool TryClassify(out ShotType shot)
        {
            shot = ShotType.None;
            IReadOnlyList<FrameableView> visible = State.Visible;

            Frameable subject = null;
            for (int i = 0; i < visible.Count; i++)
            {
                FrameableView view = visible[i];
                if (view.Frameable.Kind != FrameableKind.Prop || view.CenterVisible == false)
                    continue;

                if (subject != null)
                    return false;

                if (view.HeightFraction < minHeight || view.CenterDistance > maxCenterDistance)
                    return false;

                subject = view.Frameable;
                if (requireSingleProp == false)
                    break;
            }

            if (subject == null)
                return false;

            State.AddSubject(subject);
            shot = ShotType.CloseUpObject;
            return true;
        }
    }
}
