using System;
using UnityEngine;
using UnityEngine.Events;

namespace MagicJam.Interaction
{
    public enum InteractableType
    {
        /// <summary>Picked up on a detail shot.</summary>
        Pickup,
        /// <summary>Held item is used on it on an American shot.</summary>
        Usable,
        /// <summary>Talk to / examine on a medium shot.</summary>
        Talkable,
        /// <summary>Climb / push / jump on a full shot.</summary>
        Physical,
        /// <summary>Makes the actor flee when in focus.</summary>
        Threat
    }

    /// <summary>
    /// Object the actor can act on. The shot type decides which interaction happens (GDD section 8).
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        [SerializeField] private InteractableType type;

        [Tooltip("Where the actor stands to interact. Defaults to this transform.")]
        [SerializeField] private Transform interactionPoint;

        [Header("Usable")]
        [Tooltip("Item the actor must hold to use this object. Empty = any item (or none) works.")]
        [SerializeField] private Interactable requiredItem;
        [Tooltip("Held item is removed from the actor after a successful use.")]
        [SerializeField] private bool consumeItem = true;

        [Header("Talkable")]
        [TextArea]
        [SerializeField] private string[] lines;

        [Header("Events")]
        [SerializeField] private UnityEvent onInteracted;

        private int timesInteracted;

        public InteractableType Type => type;
        public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;
        public Interactable RequiredItem => requiredItem;
        public bool ConsumeItem => consumeItem;
        public string[] Lines => lines;
        public int TimesInteracted => timesInteracted;

        /// <summary>Raised after a successful interaction. Used by BeatTracker to tick script beats.</summary>
        public event Action<Interactable> Interacted;

        /// <summary>True when the given held item satisfies this object's requirement.</summary>
        public bool Accepts(Interactable heldItem)
        {
            return requiredItem == null || requiredItem == heldItem;
        }

        /// <summary>Called by the actor when the interaction animation completes.</summary>
        public void NotifyInteracted()
        {
            timesInteracted++;
            onInteracted?.Invoke();
            Interacted?.Invoke(this);
        }

        /// <summary>Restores interaction count (SnapshotSystem on retake).</summary>
        public void ResetState()
        {
            timesInteracted = 0;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(InteractionPoint.position, 0.15f);
            Gizmos.DrawLine(transform.position, InteractionPoint.position);
        }
    }
}
