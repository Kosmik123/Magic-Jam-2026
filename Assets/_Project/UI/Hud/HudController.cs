using System.Collections.Generic;
using MagicJam.Core;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace MagicJam.UI
{
    /// <summary>
    /// Drives the in-game HUD (Hud.uxml): camera guides, shot name, REC/cut indicator,
    /// shot timer, retake counter and the sliding script panel.
    /// Only displays state - game systems push values through the public API.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HudController : MonoBehaviour
    {
		private const string RecordingClass = "hud--recording";
        private const string CutClass = "hud--cut";
        private const string RecDotDimClass = "rec-dot--dim";
        private const string PanelHiddenClass = "script-panel--hidden";
        private const string TaskClass = "task";
        private const string TaskDoneClass = "task--done";

        [Tooltip("Optional. When set, the HUD follows the filming state (REC/cut, take timer, retakes) by itself.")]
        [SerializeField]
        private ModeController modeController;

        [SerializeField]
        private InputActionReference toggleScriptAction;

        [SerializeField]
        private bool isScriptOpen;

        [Tooltip("REC dot blink interval in seconds. 0 = no blinking.")]
        [SerializeField]
        private float recBlinkInterval = 0.5f;

        private VisualElement hud;
        private VisualElement recDot;
        private VisualElement scriptPanel;
        private VisualElement taskList;
        private Label shotLabel;
        private Label timerLabel;
        private Label retakeLabel;
        private Label sceneLabel;
        private IVisualElementScheduledItem blinkItem;

        // State is cached so it survives UIDocument rebuilds (re-enable, UXML reimport).
        [Header("States")]
        [SerializeField]
        private bool isRecording;

        [ShowNonSerializedField]
        private string shotName = "—";

        [ShowNonSerializedField]
        private float shotTime;

        [ShowNonSerializedField]
        private int retakes;

        [ShowNonSerializedField]
        private string sceneName = "";

        private readonly List<string> tasks = new List<string>();
        private readonly List<bool> taskDone = new List<bool>();

        public bool IsScriptOpen => isScriptOpen;

        private void Awake()
        {
            if (modeController == null)
                modeController = FindAnyObjectByType<ModeController>();
        }

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            hud = root.Q<VisualElement>("hud");
            recDot = root.Q<VisualElement>("recDot");
            scriptPanel = root.Q<VisualElement>("scriptPanel");
            taskList = root.Q<VisualElement>("taskList");
            shotLabel = root.Q<Label>("shotLabel");
            timerLabel = root.Q<Label>("timerLabel");
            retakeLabel = root.Q<Label>("retakeLabel");
            sceneLabel = root.Q<Label>("sceneLabel");

            if (hud == null)
            {
                Debug.LogError("HudController: 'hud' element not found - is Hud.uxml assigned to the UIDocument?", this);
                return;
            }

            if (recBlinkInterval > 0f)
                blinkItem = recDot.schedule.Execute(() => recDot.ToggleInClassList(RecDotDimClass))
                    .Every((long)(recBlinkInterval * 1000f));

            if (toggleScriptAction != null && toggleScriptAction.action != null)
            {
                toggleScriptAction.action.performed += OnToggleScript;
                toggleScriptAction.action.Enable();
            }

            if (modeController != null)
            {
                isRecording = modeController.IsRecording;
                retakes = modeController.RetakeCount;
                shotTime = modeController.TakeTime;

                modeController.ModeChanged += OnModeChanged;
                modeController.Retaken += OnRetaken;
                modeController.SecondElapsed += OnSecondElapsed;
            }

            RefreshAll();
        }

        private void OnDisable()
        {
            blinkItem?.Pause();
            blinkItem = null;

            if (modeController != null)
            {
                modeController.ModeChanged -= OnModeChanged;
                modeController.Retaken -= OnRetaken;
                modeController.SecondElapsed -= OnSecondElapsed;
            }

            if (toggleScriptAction != null && toggleScriptAction.action != null)
                toggleScriptAction.action.performed -= OnToggleScript;
        }

        /// <summary>true = Action (REC), false = Cut (pause).</summary>
        public void SetRecording(bool isRecording)
        {
            this.isRecording = isRecording;
            RefreshMode();
        }

        public void SetShotName(string name)
        {
            shotName = string.IsNullOrEmpty(name) ? "—" : name;
            if (shotLabel != null)
                shotLabel.text = shotName;
        }

        public void SetShotTime(float seconds)
        {
            shotTime = Mathf.Max(0f, seconds);
            if (timerLabel != null) 
                timerLabel.text = FormatTime(shotTime);
        }

        public void SetRetakes(int count)
        {
            retakes = Mathf.Max(0, count);
            if (retakeLabel != null)
                retakeLabel.text = $"Dubel: {retakes}";
        }

        public void SetScene(string name)
        {
            sceneName = name ?? "";
            if (sceneLabel != null) 
                sceneLabel.text = sceneName;
        }

        /// <summary>Replaces the script with a new list of tasks (all unchecked).</summary>
        public void SetTasks(IReadOnlyList<string> taskTexts)
        {
            tasks.Clear();
            taskDone.Clear();
            foreach (string text in taskTexts)
            {
                tasks.Add(text);
                taskDone.Add(false);
            }

            RebuildTasks();
        }

        public void SetTaskDone(int index, bool done)
        {
            if (index < 0 || index >= tasks.Count)
                return;

            taskDone[index] = done;
            taskList?.ElementAt(index).EnableInClassList(TaskDoneClass, done);
        }

        public void SetScriptOpen(bool open)
        {
            isScriptOpen = open;
            scriptPanel?.EnableInClassList(PanelHiddenClass, isScriptOpen == false);
        }

        public void ToggleScript() => SetScriptOpen(isScriptOpen == false);

        private void OnToggleScript(InputAction.CallbackContext _) => ToggleScript();

        private void OnModeChanged(FilmMode mode) => SetRecording(mode == FilmMode.Action);

        private void OnRetaken(int count)
        {
            SetRetakes(count);
            SetShotTime(0f);
        }

        private void OnSecondElapsed(int seconds) => SetShotTime(seconds);

        private void RefreshAll()
        {
            RefreshMode();
            SetShotName(shotName);
            SetShotTime(shotTime);
            SetRetakes(retakes);
            SetScene(sceneName);
            SetScriptOpen(isScriptOpen);
            RebuildTasks();
        }

        private void RefreshMode()
        {
            if (hud == null)
                return;

            hud.EnableInClassList(RecordingClass, isRecording);
            hud.EnableInClassList(CutClass, isRecording == false);
            recDot.RemoveFromClassList(RecDotDimClass);
            if (isRecording) 
                blinkItem?.Resume();
            else 
                blinkItem?.Pause();
        }

        private void RebuildTasks()
        {
            if (taskList == null)
                return;

            taskList.Clear();
            for (int i = 0; i < tasks.Count; i++)
            {
                var item = new VisualElement { pickingMode = PickingMode.Ignore };
                item.AddToClassList(TaskClass);
                item.EnableInClassList(TaskDoneClass, taskDone[i]);

                var box = new VisualElement { pickingMode = PickingMode.Ignore };
                box.AddToClassList("task-box");
                var label = new Label(tasks[i]) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("task-label");

                item.Add(box);
                item.Add(label);
                taskList.Add(item);
            }
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
