namespace MagicJam.CameraSystem
{
    /// <summary>
    /// Film shot sizes recognised by the game (GDD section 6).
    /// Computed by ShotDetector; everyone else only reads it.
    /// </summary>
    public enum ShotType
    {
        None,
        /// <summary>Ogólny – actor small in frame. Navigation: walk to the target in the centre of the frame.</summary>
        Wide,
        /// <summary>Pełny – whole figure visible. Physical action: climb, push, jump.</summary>
        Full,
        /// <summary>Amerykański – knees up. Use the held item on the focused object.</summary>
        American,
        /// <summary>Średni – waist up. Talk to / examine the focused object.</summary>
        Medium,
        /// <summary>Detal – interactable fills the frame. Pick the item up.</summary>
        Detail
    }
}
