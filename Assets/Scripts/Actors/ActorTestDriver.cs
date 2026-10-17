using MagicJam.CameraSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MagicJam.Actors
{
    /// <summary>
    /// Test-scene stand-in for ShotDetector and ModeController, so the actor can be tested in isolation.
    /// Space: Action/Cut, R: retake, 0-5: shot (None, Wide, Full, American, Medium, Detail),
    /// [ / ]: previous/next focus target.
    /// </summary>
    public class ActorTestDriver : MonoBehaviour
    {
        [SerializeField] private ActorController actor;
        [Tooltip("Objects that can be put in focus (Interactables, NavPoints).")]
        [SerializeField] private Transform[] focusTargets;

        private ShotType shot = ShotType.Wide;
        private int focusIndex;

        private Transform Focus =>
            focusTargets != null && focusTargets.Length > 0 ? focusTargets[focusIndex] : null;

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

            if (keyboard.digit0Key.wasPressedThisFrame) shot = ShotType.None;
            if (keyboard.digit1Key.wasPressedThisFrame) shot = ShotType.Wide;
            if (keyboard.digit2Key.wasPressedThisFrame) shot = ShotType.Full;
            if (keyboard.digit3Key.wasPressedThisFrame) shot = ShotType.American;
            if (keyboard.digit4Key.wasPressedThisFrame) shot = ShotType.Medium;
            if (keyboard.digit5Key.wasPressedThisFrame) shot = ShotType.Detail;

            int count = focusTargets != null ? focusTargets.Length : 0;
            if (count > 0)
            {
                if (keyboard.rightBracketKey.wasPressedThisFrame) focusIndex = (focusIndex + 1) % count;
                if (keyboard.leftBracketKey.wasPressedThisFrame) focusIndex = (focusIndex + count - 1) % count;
            }

            actor.SetShot(shot, Focus);
        }

        private void OnGUI()
        {
            if (actor == null)
                return;

            string held = actor.HeldItem != null ? actor.HeldItem.name : "-";
            string focus = Focus != null ? Focus.name : "-";
            GUI.Label(new Rect(10, 10, 600, 120),
                $"Mode: {(actor.IsInAction ? "AKCJA" : "CIĘCIE")}   State: {actor.State}   Ready: {actor.IsReady}\n" +
                $"Shot: {shot}   Focus: {focus}   Held: {held}\n" +
                "Space Akcja/Cięcie | R dubel | 0-5 plan | [ ] fokus");
        }
    }
}
