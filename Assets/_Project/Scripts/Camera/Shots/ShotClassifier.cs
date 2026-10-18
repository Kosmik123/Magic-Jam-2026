using System;

namespace MagicJam.CameraSystem.Shots
{
    /// <summary>
    /// One rule of shot detection. Implementations are plain serializable classes configured in the
    /// <see cref="ShotDetector"/> inspector; their own dependencies are serialized fields.
    /// </summary>
    [Serializable]
    public abstract class ShotClassifier
    {
        private ShotState state;

        protected ShotState State => state;

        /// <summary>Called once by the detector before the first detection.</summary>
        public void Initialize(ShotState shotState)
        {
            state = shotState;
        }

        /// <summary>
        /// Returns true and the shot if this rule recognizes the current frame. When it returns true it may
        /// report the objects that decided the shot with <see cref="ShotState.AddSubject"/>; when it
        /// returns false it must not add any.
        /// </summary>
        public abstract bool TryClassify(out ShotType shot);
    }
}
