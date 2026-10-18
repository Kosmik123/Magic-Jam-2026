using UnityEngine;

namespace MagicJam.Actors
{
    /// <summary>A prop the actor can take into a hand. Interacting with it picks it up.</summary>
    public class Item : Interactable
    {
        private Rigidbody body;
        private Collider[] colliders;

        public bool IsHeld { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>();
        }

        public override bool CanInteract(ActorInteractor actor)
        {
            return IsHeld == false;
        }

        public override void Interact(ActorInteractor actor)
        {
            actor.PickUp(this);
        }

        /// <summary>Puts the item into the hand: follows the socket, no physics, no collisions.</summary>
        public void Attach(Transform socket)
        {
            IsHeld = true;
            SetPhysicsEnabled(false);
            transform.SetParent(socket, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>Releases the item into the world at its current pose.</summary>
        public void Detach()
        {
            IsHeld = false;
            transform.SetParent(null, true);
            SetPhysicsEnabled(true);
        }

        private void SetPhysicsEnabled(bool value)
        {
            if (body)
                body.isKinematic = value == false;

            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = value;
        }
    }
}
