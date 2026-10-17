using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MagicJam.Interaction
{
    /// <summary>
    /// Object the actor can act on (GDD section 8). An object may carry several Interactables
    /// (e.g. roommate: Talkable + Usable).
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        [SerializeField]
        private InteractableType type;

        [Tooltip("Where the actor stands to interact. Defaults to this transform.")]
        [SerializeField]
        private Transform interactionPoint;

        [Header("Usable")]
        [Tooltip("Item the actor must hold to use this object. Empty = any held item works.")]
        [SerializeField]
        private Interactable requiredItem;

        [Tooltip("Held item is removed from the actor after a successful use.")]
        [SerializeField]
        private bool consumeItem = true;

        [Header("Talkable")]
        [TextArea]
        [SerializeField]
        private string[] lines = Array.Empty<string>();

        [Header("Events")]
        [SerializeField]
        private UnityEvent onInteracted;

        private int timesInteracted;

        public InteractableType Type => type;
        public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;
        public Interactable RequiredItem => requiredItem;
        public bool ConsumeItem => consumeItem;
        public IReadOnlyList<string> Lines => lines;
        public int TimesInteracted => timesInteracted;

        /// <summary>Raised after a successful interaction.</summary>
        public event Action<Interactable> Interacted;

        /// <summary>True when the given held item satisfies this object's requirement.</summary>
        public bool Accepts(Interactable heldItem)
        {
            return requiredItem == null || requiredItem == heldItem;
        }

        /// <summary>Called by the actor when the interaction completes.</summary>
        public void NotifyInteracted()
        {
            timesInteracted++;
            onInteracted?.Invoke();
            Interacted?.Invoke(this);
        }

        /// <summary>Restores the interaction count (e.g. on retake).</summary>
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
