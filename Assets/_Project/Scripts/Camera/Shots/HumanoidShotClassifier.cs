using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicJam.CameraSystem.Shots
{
    /// <summary>
    /// Shot of a character, decided by which of its points are in frame:
    /// head and feet = General / Full, head and knees = American, head and waist = Medium,
    /// only a big head = Close-up on the face.
    /// </summary>
    [Serializable]
    public class HumanoidShotClassifier : ShotClassifier
    {
        [Tooltip("Whole body in frame and shorter than this fraction of the frame height = General (else Full).")]
        [SerializeField]
        private float generalMaxHeight = 0.3f;

        [Tooltip("Head in frame, waist out of frame and the whole body at least this many frame heights tall = Close-up on the face.")]
        [SerializeField]
        private float closeUpFaceMinBodyHeight = 3f;

        [Tooltip("Off: the character closest to the frame center decides. On: all visible characters have to be in the same shot.")]
        [SerializeField]
        private bool requireAllCharacters;

        public override bool TryClassify(out ShotType shot)
        {
            shot = ShotType.None;
            IReadOnlyList<FrameableView> visible = State.Visible;

            ShotType main = ShotType.None;
            bool hasMain = false;
            for (int i = 0; i < visible.Count; i++)
            {
                if (visible[i].Frameable.Kind != FrameableKind.Humanoid)
                    continue;

                ShotType plan = Classify(visible[i]);
                if (hasMain == false)
                {
                    main = plan;
                    hasMain = true;
                    if (main == ShotType.None)
                        return false;
                }
                else if (requireAllCharacters && plan != main)
                {
                    return false;
                }
            }

            if (hasMain == false)
                return false;

            for (int i = 0; i < visible.Count; i++)
            {
                bool humanoid = visible[i].Frameable.Kind == FrameableKind.Humanoid;
                if (humanoid && Classify(visible[i]) == main)
                    State.AddSubject(visible[i].Frameable);
            }

            shot = main;
            return true;
        }

        private ShotType Classify(FrameableView view)
        {
            if (view.HeadVisible == false)
                return ShotType.None;

            if (view.FeetVisible)
                return view.HeightFraction < generalMaxHeight ? ShotType.General : ShotType.Full;

            if (view.KneesVisible)
                return ShotType.American;

            if (view.WaistVisible)
                return ShotType.Medium;

            if (view.HeightFraction >= closeUpFaceMinBodyHeight)
                return ShotType.CloseUpFace;

            return ShotType.None;
        }
    }
}
