using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MagicJam.Core
{
    /// <summary>
    /// Owns the filming state. The toggle action (Space) switches between Cut and Action,
    /// the retake action (R) starts a new take: it returns to Cut and bumps <see cref="RetakeCount"/>.
    /// A retake is only allowed once the current take has been recorded for <see cref="MinTakeDuration"/> seconds.
    /// Other systems subscribe to <see cref="ModeChanged"/> / <see cref="Retaken"/> / <see cref="SecondElapsed"/>
    /// instead of polling.
    /// </summary>
    public class ModeController : MonoBehaviour
	{       
        /// <summary>Raised after the mode changed; the argument is the new mode.</summary>
		public event Action<FilmMode> ModeChanged;

		/// <summary>
		/// Raised after a retake. The argument is the new <see cref="RetakeCount"/>.
		/// The mode is already back to Cut when this fires.
		/// </summary>
		public event Action<int> Retaken;

		/// <summary>
		/// Raised every full second of recording in the current take.
		/// The argument is the whole seconds recorded so far (1, 2, 3...); it restarts from 1 after a retake.
		/// </summary>
		public event Action<int> SecondElapsed;

		[SerializeField]
        private InputActionReference toggleAction;

        [SerializeField]
        private InputActionReference retakeAction;

        [Tooltip("Seconds a take has to be recorded before a retake is allowed. 0 = always allowed.")]
        [SerializeField]
        private float minTakeDuration = 3f;

        [SerializeField]
        private FilmMode startMode = FilmMode.Cut;

        private int lastTickSecond;

        public FilmMode Mode { get; private set; }

        public bool IsRecording => Mode == FilmMode.Action;

        /// <summary>How many retakes happened so far (0 = first take).</summary>
        public int RetakeCount { get; private set; }

        /// <summary>Seconds recorded in the current take (counts only in Action, resets on retake).</summary>
        public float TakeTime { get; private set; }

        public float MinTakeDuration => minTakeDuration;

        public bool CanRetake => TakeTime >= minTakeDuration;



        private void Awake()
        {
            Mode = startMode;
        }

        private void Update()
        {
            if (IsRecording == false)
                return;

            TakeTime += Time.deltaTime;
            int seconds = Mathf.FloorToInt(TakeTime);
            while (lastTickSecond < seconds)
            {
                lastTickSecond++;
                SecondElapsed?.Invoke(lastTickSecond);
            }
        }

        private void OnEnable()
        {
            Subscribe(toggleAction, OnTogglePerformed);
            Subscribe(retakeAction, OnRetakePerformed);
        }

        private void OnDisable()
        {
            Unsubscribe(toggleAction, OnTogglePerformed);
            Unsubscribe(retakeAction, OnRetakePerformed);
        }

        public void Toggle()
        {
            SetMode(IsRecording ? FilmMode.Cut : FilmMode.Action);
        }

        public void SetMode(FilmMode mode)
        {
            if (Mode == mode)
                return;

            Mode = mode;
            ModeChanged?.Invoke(mode);
        }

        /// <summary>
        /// Starts a new take: goes back to Cut, resets the take timer, increments the counter and raises
        /// <see cref="Retaken"/>. Does nothing and returns false while <see cref="CanRetake"/> is false.
        /// </summary>
        public bool Retake()
        {
            if (CanRetake == false)
                return false;

            SetMode(FilmMode.Cut);
            RetakeCount++;
            TakeTime = 0f;
            lastTickSecond = 0;
            Retaken?.Invoke(RetakeCount);
            return true;
        }

        private void OnTogglePerformed(InputAction.CallbackContext context)
        {
            Toggle();
        }

        private void OnRetakePerformed(InputAction.CallbackContext context)
        {
            Retake();
        }

        private static void Subscribe(InputActionReference reference, Action<InputAction.CallbackContext> handler)
        {
            if (reference == null || reference.action == null)
                return;

            reference.action.performed += handler;
            reference.action.Enable();
        }

        private static void Unsubscribe(InputActionReference reference, Action<InputAction.CallbackContext> handler)
        {
            if (reference == null || reference.action == null)
                return;

            reference.action.performed -= handler;
            reference.action.Disable();
        }
    }
}
