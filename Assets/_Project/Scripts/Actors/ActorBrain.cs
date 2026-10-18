using System.Collections.Generic;
using MagicJam.CameraSystem.Shots;
using MagicJam.Core;
using NaughtyAttributes;
using UnityEngine;

namespace MagicJam.Actors
{
    /// <summary>
    /// Turns the shot and the <see cref="ActorContext"/> into commands for <see cref="ActorMovement"/> and
    /// <see cref="ActorInteractor"/>. Reacts only while recording and only to shots that contain the actor.
    /// Move shots: walk to the remembered object if it is in frame, otherwise wander to a point near the frame center.
    /// Interaction shots: use the active item on the remembered object, or interact with it, once it is in reach.
    /// </summary>
    public class ActorBrain : MonoBehaviour
    {
        [SerializeField]
        private ActorContext context;

        [SerializeField]
        private ActorMovement movement;

        [SerializeField]
        private ActorInteractor interactor;

        [SerializeField]
        private ShotDetector shotDetector;

        [Tooltip("Optional. When set, the actor reacts only while recording (Action) and stops on Cut.")]
        [SerializeField]
        private ModeController modeController;

        [Tooltip("The actor's own frameable; only shots that contain it drive the actor. Empty = any shot.")]
        [SerializeField]
        private Frameable actor;

        [Header("Move")]
        [SerializeField]
        private List<ShotType> moveShots = new List<ShotType> { ShotType.General };

        [Tooltip("Seconds a move shot has to last before the actor starts walking.")]
        [SerializeField]
        private float moveShotTime = 0.1f;

        [SerializeField]
        private FrameWanderPoint wanderPoint = new FrameWanderPoint();

        [Header("Interaction")]
        [SerializeField]
        private List<ShotType> interactionShots = new List<ShotType> { ShotType.American, ShotType.Medium };

        [Tooltip("Seconds an interaction shot has to last before the actor interacts.")]
        [SerializeField]
        private float interactionShotTime = 0.5f;

        private float recordingStartTime;

        [Header("States")]
        [ShowNonSerializedField]
        private bool shotHandled;

        [ShowNonSerializedField]
        private bool approachingRememberedObject;

        private void OnEnable()
        {
            if (shotDetector)
                shotDetector.ShotChanged += OnShotChanged;

            if (modeController)
                modeController.ModeChanged += OnModeChanged;

            if (movement)
                movement.DestinationUnreachable += OnDestinationUnreachable;
        }

        private void OnDisable()
        {
            if (shotDetector)
                shotDetector.ShotChanged -= OnShotChanged;

            if (modeController)
                modeController.ModeChanged -= OnModeChanged;

            if (movement)
                movement.DestinationUnreachable -= OnDestinationUnreachable;
        }

        private void Update()
        {
            if (shotHandled || context == null || movement == null || interactor == null || shotDetector == null)
                return;

            if (modeController && modeController.IsRecording == false)
                return;

            ShotType shot = shotDetector.CurrentShotType;
            if (moveShots.Contains(shot))
            {
                if (GetStableTime() < moveShotTime)
                    return;

                shotHandled = true;
                if (IsActorInShot())
                    Move();
            }
            else if (interactionShots.Contains(shot))
            {
                if (GetStableTime() < interactionShotTime)
                    return;

                shotHandled = IsActorInShot() == false || TryInteract();
            }
            else
            {
                shotHandled = true;
            }
        }

        private float GetStableTime()
        {
            float shotTime = shotDetector.ShotTime;
            if (modeController == null)
                return shotTime;

            return Mathf.Min(shotTime, Time.time - recordingStartTime);
        }

        private bool IsActorInShot()
        {
            if (actor == null)
                return true;

            IReadOnlyList<Frameable> subjects = shotDetector.Subjects;
            for (int i = 0; i < subjects.Count; i++)
            {
                if (subjects[i] == actor)
                    return true;
            }

            return false;
        }

        private bool IsInFrame(Frameable frameable)
        {
            return shotDetector.State.TryGetView(frameable, out FrameableView view) && view.CenterVisible;
        }

        private void Move()
        {
            Frameable target = context.RememberedObject;
            if (target && IsInFrame(target))
            {
                approachingRememberedObject = movement.GoTo(target.WorldBounds.ClosestPoint(transform.position), 0f);
                return;
            }

            approachingRememberedObject = false;
            if (wanderPoint.TryGetPoint(shotDetector.State, actor, out Vector3 point))
                movement.GoTo(point);
        }

        /// <summary>Returns true when there is nothing more to do in this shot.</summary>
        private bool TryInteract()
        {
            Frameable remembered = context.RememberedObject;
            if (remembered == null || remembered.TryGetComponent(out Interactable target) == false)
                return true;

            if (IsInFrame(remembered) == false)
                return false;

            bool interacted = interactor.TryUse(context.ActiveItem, target) || interactor.TryInteract(target);
            if (interacted == false)
                return false;

            movement.Stop();
            approachingRememberedObject = false;
            context.Forget();
            return true;
        }

        private void OnShotChanged(ShotType shot)
        {
            shotHandled = false;
        }

        /// <summary>
        /// Cut stops the actor and drops its destination. Back in Action the actor reacts only to a shot that
        /// changed during the Cut, so cutting mid-walk and recording again without moving the camera keeps it standing.
        /// </summary>
        private void OnModeChanged(FilmMode mode)
        {
            if (mode == FilmMode.Action)
            {
                recordingStartTime = Time.time;
                return;
            }

            approachingRememberedObject = false;
            if (movement)
                movement.Stop();
        }

        private void OnDestinationUnreachable()
        {
            if (approachingRememberedObject == false)
                return;

            approachingRememberedObject = false;
            context.Forget();
        }
    }
}
