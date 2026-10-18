using System;
using MagicJam.CameraSystem.Shots;
using NaughtyAttributes;
using UnityEngine;

namespace MagicJam.Actors
{
    /// <summary>
    /// What the actor has in mind right now: the remembered world object, the items in its hands, the
    /// item it currently thinks about and whether it wants to put an item down. Only stores the state and
    /// raises events about its changes; <see cref="ActorContextManager"/> decides when it changes.
    /// </summary>
    public class ActorContext : MonoBehaviour
    {
        /// <summary>Raised after the remembered object changed; the argument is the new one (null = forgotten).</summary>
        public event Action<Frameable> RememberedObjectChanged;

        /// <summary>Raised after an item was taken into or removed from a hand.</summary>
        public event Action InventoryChanged;

        /// <summary>Raised after the active item changed; the argument is the new one (null = none).</summary>
        public event Action<Item> ActiveItemChanged;

        /// <summary>Raised after <see cref="WantsToPutDown"/> changed; the argument is the new value.</summary>
        public event Action<bool> WantsToPutDownChanged;

        [SerializeField]
        private Inventory inventory = new Inventory();

        [Header("States")]
        [SerializeField]
        private Frameable rememberedObject;

        [SerializeField]
        private Item activeItem;

        [SerializeField]
        private bool wantsToPutDown;

        /// <summary>Interactive object in the world the actor remembers (another actor, a prop, a door...).</summary>
        public Frameable RememberedObject => rememberedObject;

        public IReadOnlyInventory Inventory => inventory;

        /// <summary>
        /// Held item the actor thinks about: the last one taken into a hand, unless a close-up chose another
        /// held item. Always one of the items in <see cref="Inventory"/> or null.
        /// </summary>
        public Item ActiveItem => activeItem;

        /// <summary>The actor wants to put the <see cref="ActiveItem"/> down.</summary>
        public bool WantsToPutDown => wantsToPutDown;

        private void Awake()
        {
            activeItem = inventory.Get(Hand.Right);
            if (activeItem == null)
                activeItem = inventory.Get(Hand.Left);
        }

        public void Remember(Frameable target)
        {
            if (rememberedObject == target)
                return;

            rememberedObject = target;
            RememberedObjectChanged?.Invoke(target);
        }

        public void Forget()
        {
            Remember(null);
        }

        /// <summary>Forgets the remembered object and the active item; the items stay in the hands.</summary>
        public void ClearFocus()
        {
            Forget();
            SetActiveItem(null);
        }

        /// <summary>Takes the item into a free hand and makes it the active item. Returns false if both hands are full.</summary>
        public bool TryPickUp(Item item, out Hand hand)
        {
            if (inventory.TryAdd(item, out hand) == false)
                return false;

            InventoryChanged?.Invoke();
            SetActiveItem(item);
            return true;
        }

        /// <summary>Removes the item from the hand. If it was active, the item in the other hand becomes active.</summary>
        public bool Drop(Item item)
        {
            if (inventory.TryGetHand(item, out Hand hand) == false)
                return false;

            inventory.Remove(item);
            InventoryChanged?.Invoke();
            if (activeItem == item)
                SetActiveItem(inventory.Get(hand == Hand.Left ? Hand.Right : Hand.Left));

            return true;
        }

        /// <summary>Makes a held item the active one. Returns false if the actor does not hold it.</summary>
        public bool SetActiveItem(Item item)
        {
            if (item && inventory.Contains(item) == false)
                return false;

            if (activeItem == item)
                return true;

            activeItem = item;
            ActiveItemChanged?.Invoke(item);
            return true;
        }

        public void SetWantsToPutDown(bool value)
        {
            if (wantsToPutDown == value)
                return;

            wantsToPutDown = value;
            WantsToPutDownChanged?.Invoke(value);
        }
    }
}
