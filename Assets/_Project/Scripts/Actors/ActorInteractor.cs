using System;
using System.Collections;
using UnityEngine;

namespace MagicJam.Actors
{
    /// <summary>
    /// Performs interactions of the actor: interacting with an object, using a held item on an object,
    /// picking up and putting down items. Works only when the object is within reach. Decides nothing.
    /// An interaction takes time: the hand reaches the object, the effect happens on contact
    /// (<see cref="Interacted"/> / <see cref="ItemUsed"/>), then the hand returns (<see cref="InteractionFinished"/>).
    /// </summary>
    public class ActorInteractor : MonoBehaviour
    {
        /// <summary>Raised when the actor starts reaching for the object.</summary>
        public event Action<Interactable> InteractionStarted;

        /// <summary>Raised when the hand touched the object and the interaction took effect.</summary>
        public event Action<Interactable> Interacted;

        /// <summary>Raised when the hand touched the object and the held item (first argument) was used on it.</summary>
        public event Action<Item, Interactable> ItemUsed;

        /// <summary>
        /// Raised when the interaction ended. The second argument is false when it was cancelled; the effect
        /// already happened only if <see cref="Interacted"/> / <see cref="ItemUsed"/> was raised before.
        /// </summary>
        public event Action<Interactable, bool> InteractionFinished;

        [SerializeField]
        private ActorContext context;

        [SerializeField]
        private ActorMovement movement;

        [Tooltip("Distance from the actor's pivot to the surface of its body; reach is measured from there.")]
        [SerializeField]
        private float bodyRadius = 0.3f;

        [Tooltip("Optional. Gets the interactable's animation trigger when an interaction starts.")]
        [SerializeField]
        private Animator animator;

        [Header("Hands")]
        [Tooltip("Optional. Moves the hands with IK; without it the timings below are used.")]
        [SerializeField]
        private ActorHandsIK handsIK;

        [Tooltip("Parent of the item held in the left hand (a child of the hand bone).")]
        [SerializeField]
        private Transform leftHandSocket;

        [Tooltip("Parent of the item held in the right hand (a child of the hand bone).")]
        [SerializeField]
        private Transform rightHandSocket;

        [Tooltip("Seconds from the start to the contact when there is no hands IK.")]
        [SerializeField]
        private float contactDelay = 0.4f;

        [Tooltip("Seconds from the contact to the end when there is no hands IK.")]
        [SerializeField]
        private float recoverDelay = 0.3f;

        private Coroutine interaction;
        private Coroutine release;
        private Hand interactionHand;
        private int playingTrigger;

        public ActorContext Context => context;

        /// <summary>The actor is performing an interaction and cannot start another.</summary>
        public bool IsBusy => interaction != null;

        /// <summary>Object of the interaction in progress, or null.</summary>
        public Interactable CurrentTarget { get; private set; }

        private void OnDisable()
        {
            Cancel();
        }

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

        /// <summary>Starts interacting with the object. Returns false if busy or the object cannot be interacted with.</summary>
        public bool TryInteract(Interactable target)
        {
            if (IsBusy || CanInteract(target) == false)
                return false;

            Hand hand = target is Item ? context.Inventory.NextHand : GetFreeHand();
            interaction = StartCoroutine(Perform(target, null, hand));
            return true;
        }

        /// <summary>Starts using the held item on the object. Returns false if busy or the item cannot be used on it.</summary>
        public bool TryUse(Item item, Interactable target)
        {
            if (IsBusy || CanUse(item, target) == false)
                return false;

            context.Inventory.TryGetHand(item, out Hand hand);
            interaction = StartCoroutine(Perform(target, item, hand));
            return true;
        }

        /// <summary>Stops the interaction in progress; the hand returns to the animated pose.</summary>
        public void Cancel()
        {
            if (interaction == null)
                return;

            StopCoroutine(interaction);
            interaction = null;
            StopAnimation();
            Interactable target = CurrentTarget;
            CurrentTarget = null;
            if (handsIK && isActiveAndEnabled)
                release = StartCoroutine(handsIK.Release(interactionHand));
            else if (handsIK)
                handsIK.ResetHands();

            InteractionFinished?.Invoke(target, false);
        }

        private IEnumerator Perform(Interactable target, Item item, Hand hand)
        {
            if (release != null)
            {
                StopCoroutine(release);
                release = null;
            }

            CurrentTarget = target;
            interactionHand = hand;
            if (movement)
                movement.Stop();

            FaceTarget(target);
            PlayAnimation(target.AnimationTrigger);
            InteractionStarted?.Invoke(target);

            if (handsIK)
            {
                Vector3 point = target.GetInteractionPoint(handsIK.GetHandPosition(hand), out Quaternion? rotation);
                yield return handsIK.Reach(hand, point, rotation);
            }
            else
            {
                yield return new WaitForSeconds(contactDelay);
            }

            if (target)
                ApplyEffect(target, item);

            if (handsIK)
                yield return handsIK.Release(hand);
            else
                yield return new WaitForSeconds(recoverDelay);

            interaction = null;
            playingTrigger = 0;
            CurrentTarget = null;
            InteractionFinished?.Invoke(target, true);
        }

        private void PlayAnimation(string trigger)
        {
            playingTrigger = 0;
            if (animator == null || string.IsNullOrEmpty(trigger))
                return;

            int hash = Animator.StringToHash(trigger);
            if (HasTrigger(hash) == false)
            {
                Debug.LogWarning($"Animator of {name} has no trigger \"{trigger}\".", this);
                return;
            }

            playingTrigger = hash;
            animator.SetTrigger(hash);
        }

        /// <summary>Clears a trigger that was set but not consumed yet, so a cancelled interaction does not play later.</summary>
        private void StopAnimation()
        {
            if (animator && playingTrigger != 0)
                animator.ResetTrigger(playingTrigger);

            playingTrigger = 0;
        }

        private bool HasTrigger(int hash)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].nameHash == hash && parameters[i].type == AnimatorControllerParameterType.Trigger)
                    return true;
            }

            return false;
        }

        private void ApplyEffect(Interactable target, Item item)
        {
            if (item)
            {
                target.Use(item, this);
                ItemUsed?.Invoke(item, target);
                return;
            }

            target.Interact(this);
            Interacted?.Invoke(target);
        }

        private Hand GetFreeHand()
        {
            if (context.Inventory.Get(Hand.Right) == null)
                return Hand.Right;

            return context.Inventory.Get(Hand.Left) == null ? Hand.Left : Hand.Right;
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
