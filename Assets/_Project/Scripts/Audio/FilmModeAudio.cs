using MagicJam.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace MagicJam.AudioSystem
{
    /// <summary>
    /// Plays the "Action!" / "Cut!" call whenever <see cref="ModeController"/> switches the film mode.
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

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (modeController)
                modeController.ModeChanged += OnModeChanged;
        }

        private void OnDisable()
        {
            if (modeController)
                modeController.ModeChanged -= OnModeChanged;
        }

        private void OnModeChanged(FilmMode mode)
        {
            Play(mode == FilmMode.Action ? actionSound : cutSound);
        }

        private void Play(AudioResource sound)
        {
            if (sound == null)
                return;

            audioSource.Stop();
            audioSource.resource = sound;
            audioSource.Play();
        }
    }
}
