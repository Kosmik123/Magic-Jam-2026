using System;
using UnityEngine;

namespace MagicJam.CameraSystem.Shots
{
    /// <summary>The camera is being moved or turned, so there is no steady shot yet.</summary>
    [Serializable]
    public class CameraMovingShotClassifier : ShotClassifier
    {
        [SerializeField]
        private OperatorCamera operatorCamera;

        [Tooltip("Camera speed in m/s above which the camera counts as moving.")]
        [SerializeField]
        private float minSpeed = 0.3f;

        [Tooltip("Turn rate in degrees per second above which the camera counts as moving.")]
        [SerializeField]
        private float minTurnRate = 15f;

        private Quaternion lastRotation;
        private float lastTime;
        private bool hasLast;

        public override bool TryClassify(out ShotType shot)
        {
            shot = ShotType.Moving;
            if (operatorCamera == null)
                return false;

            Quaternion rotation = operatorCamera.transform.rotation;
            float now = Time.time;
            float turnRate = 0f;
            if (hasLast && now > lastTime)
                turnRate = Quaternion.Angle(lastRotation, rotation) / (now - lastTime);

            lastRotation = rotation;
            lastTime = now;
            hasLast = true;

            return operatorCamera.Velocity.sqrMagnitude > minSpeed * minSpeed || turnRate > minTurnRate;
        }
    }
}
