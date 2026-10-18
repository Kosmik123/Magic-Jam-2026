namespace MagicJam.CameraSystem.Shots
{
    /// <summary>How one <see cref="Frameable"/> looks from the camera during a single detection.</summary>
    public readonly struct FrameableView
    {
        public FrameableView(
            Frameable frameable,
            float heightFraction,
            float centerDistance,
            bool centerVisible,
            bool headVisible,
            bool waistVisible,
            bool kneesVisible,
            bool feetVisible)
        {
            Frameable = frameable;
            HeightFraction = heightFraction;
            CenterDistance = centerDistance;
            CenterVisible = centerVisible;
            HeadVisible = headVisible;
            WaistVisible = waistVisible;
            KneesVisible = kneesVisible;
            FeetVisible = feetVisible;
        }

        public Frameable Frameable { get; }

        /// <summary>Height of the whole object's bounds in frame heights (1 = fills the frame, can exceed 1).</summary>
        public float HeightFraction { get; }

        /// <summary>Viewport distance from the frame center to the object's center.</summary>
        public float CenterDistance { get; }

        /// <summary>The object's center is in frame and not hidden behind other objects.</summary>
        public bool CenterVisible { get; }

        public bool HeadVisible { get; }

        public bool WaistVisible { get; }

        public bool KneesVisible { get; }

        public bool FeetVisible { get; }
    }
}
