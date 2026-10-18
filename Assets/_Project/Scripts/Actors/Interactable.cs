using MagicJam.CameraSystem.Shots;
using UnityEngine;

namespace MagicJam.Actors
{
    /// <summary>
    /// Object in the world an actor can interact with (a prop, a door, another actor). An actor can interact
    /// only when it is within <see cref="InteractionDistance"/> of the object's bounds.
    /// </summary>
    [RequireComponent(typeof(Frameable))]
    public abstract class Interactable : MonoBehaviour
    {
        [Tooltip("Max distance from the actor's body to the object's bounds. Arm's length for props and doors, more for other actors.")]
        [SerializeField]
        private float interactionDistance = 0.5f;

        private Frameable frameable;

        public float InteractionDistance => interactionDistance;

        public Frameable Frameable
        {
            get
            {
                if (frameable == null)
                    frameable = GetComponent<Frameable>();

                return frameable;
            }
        }

        /// <summary>Point of the object's bounds closest to the position.</summary>
        public Vector3 ClosestPoint(Vector3 position)
        {
            return Frameable.WorldBounds.ClosestPoint(position);
        }

        public virtual bool CanInteract(ActorInteractor actor)
        {
            return true;
        }

        /// <summary>Called by <see cref="ActorInteractor"/> once it checked the actor can reach the object.</summary>
        public abstract void Interact(ActorInteractor actor);

        /// <summary>Whether the held item can be used on this object.</summary>
        public virtual bool CanUse(Item item, ActorInteractor actor)
        {
            return false;
        }

        /// <summary>Called by <see cref="ActorInteractor"/> when the actor uses a held item on this object.</summary>
        public virtual void Use(Item item, ActorInteractor actor)
        {
        }
    }
}
