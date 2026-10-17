using MagicJam.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MagicJam.Actors
{
    /// <summary>
    /// Test-scene driver that sends commands straight to the actor, so it can be tested in isolation.
    /// Every action is optional – unassigned ones are ignored.
    /// </summary>
    public class ActorTestDriver : MonoBehaviour
    {
        [SerializeField]
        private ActorController actor;

        [Tooltip("Command targets (Interactables, NavPoints or plain transforms).")]
        [SerializeField]
        private Transform[] targets;

        [Header("Input")]
        [Tooltip("Toggles Action / Cut.")]
        [SerializeField]
        private InputActionReference actionCutAction;

        [SerializeField]
        private InputActionReference retakeAction;

        [SerializeField]
        private InputActionReference goToAction;

        [SerializeField]
        private InputActionReference interactAction;

        [SerializeField]
        private InputActionReference fleeAction;

        [SerializeField]
        private InputActionReference previousTargetAction;

        [SerializeField]
        private InputActionReference nextTargetAction;

        private int targetIndex;

        private int TargetCount => targets != null ? targets.Length : 0;
        private Transform Target => TargetCount > 0 ? targets[targetIndex] : null;

        private void OnEnable()
        {
            if (actor == null)
                return;

            Subscribe(actionCutAction, OnActionCut);
            Subscribe(retakeAction, OnRetake);
            Subscribe(goToAction, OnGoTo);
            Subscribe(interactAction, OnInteract);
            Subscribe(fleeAction, OnFlee);
            Subscribe(previousTargetAction, OnPreviousTarget);
            Subscribe(nextTargetAction, OnNextTarget);
        }

        private void OnDisable()
        {
            Unsubscribe(actionCutAction, OnActionCut);
            Unsubscribe(retakeAction, OnRetake);
            Unsubscribe(goToAction, OnGoTo);
            Unsubscribe(interactAction, OnInteract);
            Unsubscribe(fleeAction, OnFlee);
            Unsubscribe(previousTargetAction, OnPreviousTarget);
            Unsubscribe(nextTargetAction, OnNextTarget);
        }

        private void OnActionCut(InputAction.CallbackContext context)
        {
            if (actor.IsInAction)
                actor.Cut();
            else
                actor.StartAction();
        }

        private void OnRetake(InputAction.CallbackContext context)
        {
            actor.Retake();
        }

        private void OnGoTo(InputAction.CallbackContext context)
        {
            var target = Target;
            if (target == null)
                return;

            var navPoint = target.GetComponent<NavPoint>();
            actor.GoTo(navPoint != null ? navPoint.Destination.position : target.position);
        }

        private void OnInteract(InputAction.CallbackContext context)
        {
            if (Target != null)
                actor.Interact(Target.GetComponent<Interactable>());
        }

        private void OnFlee(InputAction.CallbackContext context)
        {
            if (Target != null)
                actor.FleeFrom(Target.position);
        }

        private void OnPreviousTarget(InputAction.CallbackContext context)
        {
            if (TargetCount > 0)
                targetIndex = (targetIndex + TargetCount - 1) % TargetCount;
        }

        private void OnNextTarget(InputAction.CallbackContext context)
        {
            if (TargetCount > 0)
                targetIndex = (targetIndex + 1) % TargetCount;
        }

        private static void Subscribe(InputActionReference reference, System.Action<InputAction.CallbackContext> handler)
        {
            if (reference == null || reference.action == null)
                return;
            reference.action.performed += handler;
            reference.action.Enable();
        }

        private static void Unsubscribe(InputActionReference reference, System.Action<InputAction.CallbackContext> handler)
        {
            if (reference == null || reference.action == null)
                return;
            reference.action.performed -= handler;
        }

        private void OnGUI()
        {
            if (actor == null)
                return;

            string held = actor.HeldItem != null ? actor.HeldItem.name : "-";
            string target = Target != null ? Target.name : "-";
            GUI.Label(new Rect(10, 10, 600, 60),
                $"Mode: {(actor.IsInAction ? "AKCJA" : "CIĘCIE")}   State: {actor.State}   Ready: {actor.IsReady}\n" +
                $"Target: {target}   Held: {held}");
        }
    }
}
