using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MagicJam.Core
{
    /// <summary>
    /// Owns the filming state. The toggle action (Space) switches between Cut and Action,
    /// the retake action (R) starts a new take: it returns to Cut and bumps <see cref="RetakeCount"/>.
    /// Other systems subscribe to <see cref="ModeChanged"/> / <see cref="Retaken"/> instead of polling.
    /// </summary>
    public class ModeController : MonoBehaviour
    {
        [SerializeField]
        private InputActionReference toggleAction;

        [SerializeField]
        private InputActionReference retakeAction;

        [SerializeField]
        private FilmMode startMode = FilmMode.Cut;

        public FilmMode Mode { get; private set; }

        public bool IsRecording => Mode == FilmMode.Action;

        /// <summary>How many retakes happened so far (0 = first take).</summary>
        public int RetakeCount { get; private set; }

        /// <summary>Raised after the mode changed; the argument is the new mode.</summary>
        public event Action<FilmMode> ModeChanged;

        /// <summary>
        /// Raised after a retake. The argument is the new <see cref="RetakeCount"/>.
        /// The mode is already back to Cut when this fires.
        /// </summary>
        public event Action<int> Retaken;

        private void Awake()
        {
            Mode = startMode;
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

        /// <summary>Starts a new take: goes back to Cut, increments the counter and raises <see cref="Retaken"/>.</summary>
        public void Retake()
        {
            SetMode(FilmMode.Cut);
            RetakeCount++;
            Retaken?.Invoke(RetakeCount);
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
