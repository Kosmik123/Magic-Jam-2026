using System.Collections.Generic;
using MagicJam.CameraSystem.Shots;
using MagicJam.Core;
using UnityEngine;

namespace MagicJam.Actors
{
    /// <summary>
    /// Makes the actor interact with an object when the camera holds one of the given shots on a subject while
    /// recording, e.g. a close-up on the sleeping actor's face makes it get out of bed. Fires once per shot.
    /// </summary>
    public class ShotInteractionTrigger : MonoBehaviour
    {
        [SerializeField]
        private ActorInteractor interactor;

        [SerializeField]
        private Interactable target;

        [SerializeField]
        private ShotDetector shotDetector;

        [Tooltip("Optional. When set, the trigger works only while recording (Action).")]
        [SerializeField]
        private ModeController modeController;

        [Tooltip("Main subject of the shot, usually the actor itself. Empty = any.")]
        [SerializeField]
        private Frameable subject;

        [SerializeField]
        private List<ShotType> shots = new List<ShotType> { ShotType.CloseUpFace };

        [Tooltip("Seconds the shot has to last (while recording) before the actor interacts.")]
        [SerializeField]
        private float stableShotTime = 0.5f;

        private float recordingStartTime;

        [Header("States")]
        [SerializeField]
        private bool shotHandled;

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
            if (shotHandled || interactor == null || target == null || shotDetector == null)
                return;

            if (modeController && modeController.IsRecording == false)
                return;

            if (shots.Contains(shotDetector.CurrentShotType) == false || (subject && shotDetector.MainSubject != subject))
            {
                shotHandled = true;
                return;
            }

            if (GetStableTime() < stableShotTime)
                return;

            // A busy actor tries again once it is free; one that cannot interact gives up for this shot.
            shotHandled = interactor.TryInteract(target) || interactor.IsBusy == false;
        }

        private float GetStableTime()
        {
            float shotTime = shotDetector.ShotTime;
            if (modeController == null)
                return shotTime;

            return Mathf.Min(shotTime, Time.time - recordingStartTime);
        }

        private void OnShotChanged(ShotType shot)
        {
            shotHandled = false;
        }

        private void OnModeChanged(FilmMode mode)
        {
            if (mode != FilmMode.Action)
                return;

            recordingStartTime = Time.time;
            shotHandled = false;
        }
    }
}
