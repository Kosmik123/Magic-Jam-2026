using System.Collections.Generic;
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

        [SerializeField] private InputActionReference toggleScriptAction;
        [SerializeField] private bool scriptOpenOnStart;
        [Tooltip("REC dot blink interval in seconds. 0 = no blinking.")]
        [SerializeField] private float recBlinkInterval = 0.5f;

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
        private bool recording;
        private bool scriptOpen;
        private string shotName = "—";
        private float shotTime;
        private int retakes;
        private string sceneName = "";
        private readonly List<string> tasks = new List<string>();
        private readonly List<bool> taskDone = new List<bool>();

        public bool IsScriptOpen => scriptOpen;

        private void Awake()
        {
            scriptOpen = scriptOpenOnStart;
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

            RefreshAll();
        }

        private void OnDisable()
        {
            blinkItem?.Pause();
            blinkItem = null;

            if (toggleScriptAction != null && toggleScriptAction.action != null)
                toggleScriptAction.action.performed -= OnToggleScript;
        }

        /// <summary>true = Action (REC), false = Cut (pause).</summary>
        public void SetRecording(bool isRecording)
        {
            recording = isRecording;
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
            scriptOpen = open;
            scriptPanel?.EnableInClassList(PanelHiddenClass, !scriptOpen);
        }

        public void ToggleScript() => SetScriptOpen(!scriptOpen);

        private void OnToggleScript(InputAction.CallbackContext _) => ToggleScript();

        private void RefreshAll()
        {
            RefreshMode();
            SetShotName(shotName);
            SetShotTime(shotTime);
            SetRetakes(retakes);
            SetScene(sceneName);
            SetScriptOpen(scriptOpen);
            RebuildTasks();
        }

        private void RefreshMode()
        {
            if (hud == null)
                return;

            hud.EnableInClassList(RecordingClass, recording);
            hud.EnableInClassList(CutClass, !recording);
            recDot.RemoveFromClassList(RecDotDimClass);
            if (recording) 
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
