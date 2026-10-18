using System.Collections;
using MagicJam.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace MagicJam.AudioSystem
{
    /// <summary>
    /// Plays the "Action!" / "Cut!" call whenever <see cref="ModeController"/> switches the film mode.
    /// After a retake the retake call plays once the Cut sound has finished.
    /// The sounds are <see cref="AudioResource"/>s, so both single clips and Audio Random Containers work.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class FilmModeAudio : MonoBehaviour
    {
        [SerializeField]
        private ModeController modeController;

        [SerializeField]
        private AudioResource actionSound;

        [SerializeField]
        private AudioResource cutSound;

        [SerializeField]
        private AudioResource retakeSound;

        private AudioSource audioSource;
        private Coroutine pendingRetake;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (modeController == null)
                return;

            modeController.ModeChanged += OnModeChanged;
            modeController.Retaken += OnRetaken;
        }

        private void OnDisable()
        {
            CancelPendingRetake();
            if (modeController == null)
                return;

            modeController.ModeChanged -= OnModeChanged;
            modeController.Retaken -= OnRetaken;
        }

        private void OnModeChanged(FilmMode mode)
        {
            Play(mode == FilmMode.Action ? actionSound : cutSound);
        }

        private void OnRetaken(int retakeCount)
        {
            CancelPendingRetake();
            pendingRetake = StartCoroutine(PlayRetakeAfterCurrent());
        }

        private IEnumerator PlayRetakeAfterCurrent()
        {
            while (audioSource.isPlaying)
                yield return null;

            pendingRetake = null;
            Play(retakeSound);
        }

        private void CancelPendingRetake()
        {
            if (pendingRetake == null)
                return;

            StopCoroutine(pendingRetake);
            pendingRetake = null;
        }

        private void Play(AudioResource sound)
        {
            CancelPendingRetake();
            if (sound == null)
                return;

            audioSource.Stop();
            audioSource.resource = sound;
            audioSource.Play();
        }
    }
}
