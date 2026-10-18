using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace MagicJam.CameraSystem.Shots
{
    /// <summary>
    /// Detects which shot the camera is framing by asking the classifiers in order; the first one that
    /// recognizes the frame wins, otherwise the default shot is used. It does not know what a shot means.
    /// </summary>
    public class ShotDetector : MonoBehaviour
    {
        /// <summary>Raised when the detected shot changed; the argument is the new shot.</summary>
        public event Action<ShotType> ShotChanged;

        [SerializeField]
        private ShotState state;

        [Tooltip("Shot used when no classifier recognizes the frame.")]
        [SerializeField]
        private ShotType defaultShotType = ShotType.None;

        [Tooltip("Seconds between detections.")]
        [SerializeField]
        private float evaluationInterval = 0.1f;

        [Tooltip("Checked from top to bottom; the first one that recognizes the frame decides.")]
        [SerializeReference]
        [SubclassSelector]
        private List<ShotClassifier> classifiers = new List<ShotClassifier>();

        private float nextEvaluationTime;
        private float shotStartTime;

        [Header("States")]
        [ShowNonSerializedField]
        private ShotType currentShotType;

        public ShotType CurrentShotType => currentShotType;

        /// <summary>Seconds the current shot has lasted without changing.</summary>
        [ShowNativeProperty]
        public float ShotTime => Time.time - shotStartTime;

        /// <summary>Objects that decided the current shot, the one closest to the frame center first.</summary>
        public IReadOnlyList<Frameable> Subjects => state.Subjects;

        private void Awake()
        {
            for (int i = 0; i < classifiers.Count; i++)
                classifiers[i]?.Initialize(state);

            currentShotType = defaultShotType;
            shotStartTime = Time.time;
        }

        private void Update()
        {
            if (Time.time < nextEvaluationTime)
                return;

            nextEvaluationTime = Time.time + evaluationInterval;
            Evaluate();
        }

        private void Evaluate()
        {
            state.Refresh();
            state.ClearSubjects();

            ShotType detected = defaultShotType;
            for (int i = 0; i < classifiers.Count; i++)
            {
                if (classifiers[i] == null)
                    continue;

                if (classifiers[i].TryClassify(out ShotType shot))
                {
                    detected = shot;
                    break;
                }
            }

            if (detected == currentShotType)
                return;

            currentShotType = detected;
            shotStartTime = Time.time;
            ShotChanged?.Invoke(detected);
        }
    }
}
