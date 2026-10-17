namespace MagicJam.Actors
{
    /// <summary>
    /// States of the actor state machine (GDD section 7).
    /// </summary>
    public enum ActorState
    {
        /// <summary>Standing still (on a mark or after finishing a command).</summary>
        Idle,
        /// <summary>Walking with the NavMeshAgent to a target (GoTo or approaching an interactable).</summary>
        MoveTo,
        /// <summary>Performing an interaction on an Interactable.</summary>
        Interact,
        /// <summary>Running away from a threat.</summary>
        Flee,
        /// <summary>Break mode: deterministic list of off-duty behaviours.</summary>
        OffDuty,
        /// <summary>Walking back to the starting mark of the take.</summary>
        ReturnToMark,
        /// <summary>Funny failure, the take is ruined.</summary>
        Fail
    }
}
