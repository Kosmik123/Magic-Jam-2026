using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MagicJam.Core
{
    /// <summary>
    /// Owns the filming state. The toggle action (Space) switches between Cut and Action.
    /// Other systems subscribe to <see cref="ModeChanged"/> instead of polling the mode.
    /// </summary>
    public class ModeController : MonoBehaviour
    {
        [SerializeField]
        private InputActionReference toggleAction;

        [SerializeField]
        private FilmMode startMode = FilmMode.Cut;

        public FilmMode Mode { get; private set; }

        public bool IsRecording => Mode == FilmMode.Action;

        /// <summary>Raised after the mode changed; the argument is the new mode.</summary>
        public event Action<FilmMode> ModeChanged;

        private void Awake()
        {
            Mode = startMode;
        }

        private void OnEnable()
        {
            if (toggleAction == null || toggleAction.action == null)
                return;

            toggleAction.action.performed += OnTogglePerformed;
            toggleAction.action.Enable();
        }

        private void OnDisable()
        {
            if (toggleAction == null || toggleAction.action == null)
                return;

            toggleAction.action.performed -= OnTogglePerformed;
            toggleAction.action.Disable();
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

        private void OnTogglePerformed(InputAction.CallbackContext context)
        {
            Toggle();
        }
    }
}
