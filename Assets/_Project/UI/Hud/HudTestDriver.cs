using UnityEngine;
using UnityEngine.InputSystem;

namespace MagicJam.UI
{
    /// <summary>
    /// Test-scene only: fakes game state so the HUD can be checked in isolation.
    /// Space = Action/Cut, R = retake, 1-9 = toggle task, P = next shot name.
    /// Remove once ModeController / ShotDetector drive the HUD.
    /// </summary>
    [RequireComponent(typeof(HudController))]
    public class HudTestDriver : MonoBehaviour
    {
        [SerializeField]
        private string sceneName = "Scena 1 – Pobudka";

        [SerializeField]
        private string[] tasks =
        {
            "Aktor wstaje z łóżka.",
            "Aktor idzie do okna i odsłania zasłony.",
        };

        [SerializeField]
        private string[] shotNames =
        {
            "PLAN OGÓLNY", "PLAN PEŁNY", "PLAN AMERYKAŃSKI", "PLAN ŚREDNI", "DETAL",
        };

        private HudController hud;

        [Header("States")]
        [SerializeField]
        private bool recording;

        [SerializeField]
        private float shotTime;

        [SerializeField]
        private int retakes;

        [SerializeField]
        private int shotIndex;

        private bool[] done;

        private void Start()
        {
            hud = GetComponent<HudController>();
            done = new bool[tasks.Length];
            hud.SetScene(sceneName);
            hud.SetTasks(tasks);
            hud.SetShotName(shotNames.Length > 0 ? shotNames[0] : null);
            hud.SetRecording(false);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null)
                return;

            if (kb.spaceKey.wasPressedThisFrame)
            {
                recording = !recording;
                hud.SetRecording(recording);
            }

            if (kb.rKey.wasPressedThisFrame)
            {
                recording = false;
                shotTime = 0f;
                hud.SetShotTime(0f);
                hud.SetRetakes(++retakes);
                hud.SetRecording(false);
                for (int i = 0; i < done.Length; i++)
                    hud.SetTaskDone(i, done[i] = false);
            }

            if (kb.pKey.wasPressedThisFrame && shotNames.Length > 0)
            {
                shotIndex = (shotIndex + 1) % shotNames.Length;
                hud.SetShotName(shotNames[shotIndex]);
            }

            for (int i = 0; i < done.Length && i < 9; i++)
            {
                if (kb[Key.Digit1 + i].wasPressedThisFrame)
                    hud.SetTaskDone(i, done[i] = !done[i]);
            }

            if (recording)
            {
                shotTime += Time.deltaTime;
                hud.SetShotTime(shotTime);
            }
        }
    }
}
