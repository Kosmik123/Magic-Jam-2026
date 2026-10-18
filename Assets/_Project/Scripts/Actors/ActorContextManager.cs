using MagicJam.CameraSystem.Shots;
using MagicJam.Core;
using NaughtyAttributes;
using UnityEngine;

namespace MagicJam.Actors
{
    /// <summary>
    /// Changes the <see cref="ActorContext"/> based on the shot from <see cref="ShotDetector"/>. A shot changes
    /// the context once, after it has lasted <see cref="StableShotTime"/> seconds while recording:
    /// a close-up on a held item makes it the active item, a close-up on any other object makes the actor
    /// remember it, a close-up on the actor's face makes it forget both the remembered object and the active item.
    /// </summary>
    public class ActorContextManager : MonoBehaviour
    {
        [SerializeField]
        private ActorContext context;

        [SerializeField]
        private ShotDetector shotDetector;

        [Tooltip("Optional. When set, the context changes only while recording (Action); framing during Cut is ignored.")]
        [SerializeField]
        private ModeController modeController;

        [Tooltip("The actor the context belongs to. A close-up on its face clears the remembered object and the active item. Empty = any face.")]
        [SerializeField]
        private Frameable actor;

        [Tooltip("Seconds a shot has to last (while recording) before it changes the context.")]
        [SerializeField]
        private float stableShotTime = 0.5f;

        private float recordingStartTime;

        [Header("States")]
        [ShowNonSerializedField]
        private bool shotApplied;

        public float StableShotTime => stableShotTime;

        private void OnEnable()
        {
            if (shotDetector)
                shotDetector.ShotChanged += OnShotChanged;

            if (modeController)
                modeController.ModeChanged += OnModeChanged;
        }

        private void OnDisable()
        {
            if (shotDetector)
                shotDetector.ShotChanged -= OnShotChanged;

            if (modeController)
                modeController.ModeChanged -= OnModeChanged;
        }

        private void Update()
        {
            if (shotApplied || context == null || shotDetector == null)
                return;

            if (modeController && modeController.IsRecording == false)
                return;

            if (GetStableTime() < stableShotTime)
                return;

            shotApplied = true;
            Apply(shotDetector.CurrentShotType, shotDetector.MainSubject);
        }

        private float GetStableTime()
        {
            float shotTime = shotDetector.ShotTime;
            if (modeController == null)
                return shotTime;

            return Mathf.Min(shotTime, Time.time - recordingStartTime);
        }

        private void Apply(ShotType shot, Frameable subject)
        {
            switch (shot)
            {
                case ShotType.CloseUpObject:
                    ApplyObjectCloseUp(subject);
                    break;

                case ShotType.CloseUpFace:
                    if (actor == null || subject == actor)
                        context.ClearFocus();
                    break;
            }
        }

        private void ApplyObjectCloseUp(Frameable subject)
        {
            if (subject == null)
                return;

            if (subject.TryGetComponent(out Item item) && context.Inventory.Contains(item))
                context.SetActiveItem(item);
            else
                context.Remember(subject);
        }

        private void OnShotChanged(ShotType shot)
        {
            shotApplied = false;
        }

        private void OnModeChanged(FilmMode mode)
        {
            if (mode != FilmMode.Action)
                return;

            recordingStartTime = Time.time;
            shotApplied = false;
        }
    }
}
