namespace MagicJam.CameraSystem.Shots
{
    public enum FrameableKind
    {
        /// <summary>An object: only its bounds matter.</summary>
        Prop,

        /// <summary>A character: head, waist, knees and feet are checked for visibility.</summary>
        Humanoid,
    }
}
