namespace MagicJam.Interaction
{
    /// <summary>
    /// Kind of interaction an <see cref="Interactable"/> offers (GDD section 8).
    /// </summary>
    public enum InteractableType
    {
        /// <summary>Picked up by the actor.</summary>
        Pickup,
        /// <summary>The actor uses its held item on it.</summary>
        Usable,
        /// <summary>The actor talks to / examines it.</summary>
        Talkable,
        /// <summary>The actor climbs / pushes / jumps on it.</summary>
        Physical,
        /// <summary>Something the actor flees from.</summary>
        Threat
    }
}
