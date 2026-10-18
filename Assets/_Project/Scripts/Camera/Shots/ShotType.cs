namespace MagicJam.CameraSystem.Shots
{
    /// <summary>
    /// Kind of shot the camera is currently framing. The detector only reports it;
    /// what a shot means for the actors is decided by other systems.
    /// </summary>
    public enum ShotType
    {
        None,
        Moving,
        General,
        Full,
        American,
        Medium,
        CloseUpFace,
        CloseUpObject,
    }
}
