using System.Collections;
using UnityEngine;

namespace MagicJam.Actors
{
    /// <summary>
    /// Extra action the actor performs during an interaction with an object, e.g. getting out of bed along a path.
    /// Referenced by the <see cref="Interactable"/>; <see cref="ActorInteractor"/> runs it alongside the
    /// interaction animation and the interaction lasts until the action ends.
    /// </summary>
    public abstract class InteractionAction : MonoBehaviour
    {
        [Tooltip("On: the hand also reaches the object and the effect happens on contact. Off: no reach, the effect happens when the action ends.")]
        [SerializeField]
        private bool usesHands;

        [Tooltip("Off: Cut does not stop the action, the actor always finishes it.")]
        [SerializeField]
        private bool interruptible = true;

        [Tooltip("Optional. Bool parameter of the actor's Animator set when the action starts, e.g. \"Asleep\".")]
        [SerializeField]
        private string animatorBool;

        [SerializeField]
        private bool animatorBoolValue;

        public bool UsesHands => usesHands;

        /// <summary>Bool parameter of the actor's Animator set when the action starts; empty = none.</summary>
        public string AnimatorBool => animatorBool;

        public bool AnimatorBoolValue => animatorBoolValue;

        /// <summary>Whether <see cref="ActorInteractor.Cancel"/> can stop the action before it ends.</summary>
        public bool Interruptible => interruptible;

        /// <summary>Runs the action on the actor; the interaction waits until it ends.</summary>
        public abstract IEnumerator Perform(ActorInteractor actor);

        /// <summary>Called when the interaction was stopped before the action ended.</summary>
        public virtual void Stop(ActorInteractor actor)
        {
        }
    }
}
