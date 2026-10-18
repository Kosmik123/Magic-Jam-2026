using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MagicJam.Actors
{
    /// <summary>
    /// Interactable configured in the Inspector: raises a UnityEvent on interaction and on using one of the
    /// accepted items. Good enough for simple objects (a switch, a door) without writing a new class.
    /// </summary>
    public class EventInteractable : Interactable
    {
        [SerializeField]
        private bool interactable = true;

        [SerializeField]
        private UnityEvent interacted;

        [Tooltip("Items that can be used on this object. Empty = none.")]
        [SerializeField]
        private List<Item> acceptedItems = new List<Item>();

        [SerializeField]
        private UnityEvent<Item> itemUsed;

        /// <summary>Turns interacting on or off, e.g. from <c>interacted</c> to make a one-time interaction.</summary>
        public void SetInteractable(bool value)
        {
            interactable = value;
        }

        public override bool CanInteract(ActorInteractor actor)
        {
            return interactable;
        }

        public override void Interact(ActorInteractor actor)
        {
            interacted.Invoke();
        }

        public override bool CanUse(Item item, ActorInteractor actor)
        {
            return acceptedItems.Contains(item);
        }

        public override void Use(Item item, ActorInteractor actor)
        {
            itemUsed.Invoke(item);
        }
    }
}
