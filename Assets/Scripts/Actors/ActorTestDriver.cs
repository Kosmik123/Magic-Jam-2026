using MagicJam.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MagicJam.Actors
{
    /// <summary>
    /// Test-scene driver that sends commands to the actor directly, so it can be tested in isolation.
    /// Space: Action/Cut, R: retake, G: go to target, I: interact with target, F: flee from target,
    /// [ / ]: previous/next target.
    /// </summary>
    public class ActorTestDriver : MonoBehaviour
    {
        [SerializeField] private ActorController actor;
        [Tooltip("Command targets (Interactables, NavPoints or plain transforms).")]
        [SerializeField] private Transform[] targets;

        private int targetIndex;

        private Transform Target =>
            targets != null && targets.Length > 0 ? targets[targetIndex] : null;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || actor == null)
                return;

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                if (actor.IsInAction) actor.Cut();
                else actor.StartAction();
            }
            if (keyboard.rKey.wasPressedThisFrame)
                actor.Retake();

            int count = targets != null ? targets.Length : 0;
            if (count == 0)
                return;

            if (keyboard.rightBracketKey.wasPressedThisFrame) targetIndex = (targetIndex + 1) % count;
            if (keyboard.leftBracketKey.wasPressedThisFrame) targetIndex = (targetIndex + count - 1) % count;

            var target = Target;
            if (target == null)
                return;

            if (keyboard.gKey.wasPressedThisFrame)
            {
                var navPoint = target.GetComponent<NavPoint>();
                actor.GoTo(navPoint != null ? navPoint.Destination.position : target.position);
            }
            if (keyboard.iKey.wasPressedThisFrame)
                actor.Interact(target.GetComponent<Interactable>());
            if (keyboard.fKey.wasPressedThisFrame)
                actor.FleeFrom(target.position);
        }

        private void OnGUI()
        {
            if (actor == null)
                return;

            string held = actor.HeldItem != null ? actor.HeldItem.name : "-";
            string target = Target != null ? Target.name : "-";
            GUI.Label(new Rect(10, 10, 600, 120),
                $"Mode: {(actor.IsInAction ? "AKCJA" : "CIĘCIE")}   State: {actor.State}   Ready: {actor.IsReady}\n" +
                $"Target: {target}   Held: {held}\n" +
                "Space Akcja/Cięcie | R dubel | G idź | I interakcja | F ucieczka | [ ] cel");
        }
    }
}
