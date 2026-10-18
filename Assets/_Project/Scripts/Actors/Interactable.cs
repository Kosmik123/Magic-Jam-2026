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

        [Tooltip("Where the hand reaches, e.g. a door handle; its rotation is used for the hand. Empty = the point of the bounds closest to the hand.")]
        [SerializeField]
        private Transform interactionPoint;

        [Tooltip("Optional. Trigger parameter of the actor's Animator set when the interaction starts, e.g. \"OpenDoor\".")]
        [SerializeField]
        private string animationTrigger;

        [Tooltip("Optional. Extra action the actor performs during the interaction, e.g. moving along a path.")]
        [SerializeField]
        private InteractionAction action;

        private Frameable frameable;

        public float InteractionDistance => interactionDistance;

        /// <summary>Animator trigger the actor plays during the interaction; empty = IK only.</summary>
        public string AnimationTrigger => animationTrigger;

        /// <summary>Extra action the actor performs during the interaction, or null.</summary>
        public InteractionAction Action => action;

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

        /// <summary>Point the hand reaches to; the rotation is null when the hand should keep its own.</summary>
        public Vector3 GetInteractionPoint(Vector3 handPosition, out Quaternion? rotation)
        {
            if (interactionPoint)
            {
                rotation = interactionPoint.rotation;
                return interactionPoint.position;
            }

            rotation = null;
            return ClosestPoint(handPosition);
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
