using System;
using UnityEngine;

namespace MagicJam.Actors
{
    /// <summary>
    /// Performs interactions of the actor: interacting with an object, using a held item on an object,
    /// picking up and putting down items. Works only when the object is within reach. Decides nothing.
    /// </summary>
    public class ActorInteractor : MonoBehaviour
    {
        /// <summary>Raised after the actor interacted with the object.</summary>
        public event Action<Interactable> Interacted;

        /// <summary>Raised after the actor used the held item (first argument) on the object.</summary>
        public event Action<Item, Interactable> ItemUsed;

        [SerializeField]
        private ActorContext context;

        [SerializeField]
        private ActorMovement movement;

        [Tooltip("Distance from the actor's pivot to the surface of its body; reach is measured from there.")]
        [SerializeField]
        private float bodyRadius = 0.3f;

        [Header("Hands")]
        [SerializeField]
        private Transform leftHandSocket;

        [SerializeField]
        private Transform rightHandSocket;

        public ActorContext Context => context;

        /// <summary>Horizontal distance from the actor's body to the object's bounds.</summary>
        public float GetDistanceTo(Interactable target)
        {
            Vector3 position = transform.position;
            Vector3 offset = target.ClosestPoint(position) - position;
            offset.y = 0f;
            return Mathf.Max(0f, offset.magnitude - bodyRadius);
        }

        public bool CanReach(Interactable target)
        {
            return target && GetDistanceTo(target) <= target.InteractionDistance;
        }

        public bool CanInteract(Interactable target)
        {
            return CanReach(target) && target.CanInteract(this);
        }

        public bool CanUse(Item item, Interactable target)
        {
            return item && context.Inventory.Contains(item) && CanReach(target) && target.CanUse(item, this);
        }

        public bool TryInteract(Interactable target)
        {
            if (CanInteract(target) == false)
                return false;

            FaceTarget(target);
            target.Interact(this);
            Interacted?.Invoke(target);
            return true;
        }

        public bool TryUse(Item item, Interactable target)
        {
            if (CanUse(item, target) == false)
                return false;

            FaceTarget(target);
            target.Use(item, this);
            ItemUsed?.Invoke(item, target);
            return true;
        }

        /// <summary>Takes the item into a hand. With both hands full, the item taken first is dropped before.</summary>
        public bool PickUp(Item item)
        {
            if (context.Inventory.IsFull && context.Inventory.Contains(item) == false)
                PutDown(context.Inventory.Oldest);

            if (context.TryPickUp(item, out Hand hand) == false)
                return false;

            item.Attach(GetSocket(hand));
            return true;
        }

        /// <summary>Releases the held item into the world where it is now.</summary>
        public bool PutDown(Item item)
        {
            if (context.Drop(item) == false)
                return false;

            item.Detach();
            return true;
        }

        private Transform GetSocket(Hand hand)
        {
            Transform socket = hand == Hand.Left ? leftHandSocket : rightHandSocket;
            return socket ? socket : transform;
        }

        private void FaceTarget(Interactable target)
        {
            if (movement)
                movement.Face(target.ClosestPoint(transform.position));
        }
    }
}
