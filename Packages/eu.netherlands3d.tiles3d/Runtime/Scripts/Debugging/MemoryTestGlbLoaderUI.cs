using System;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Tiles3D
{
    [RequireComponent(typeof(UIDocument))]
    public class MemoryTestGlbLoaderUI : MonoBehaviour
    {
        [SerializeField] private MemoryTestGlbLoader loader;
        private VisualTreeAsset layout;
        private StyleSheet styleSheet;

        UIDocument uiDocument;
        Button startButton;
        Button stopButton;
        Label statusLabel;
        Label progressLabel;
        DropdownField csvDropdown;
        TextField apiKeyField;
        Label heapLabel;
        Label gcHeapLabel;
        MemoryStatsGraph memoryGraph;
        EventCallback<ChangeEvent<string>> apiKeyChangedHandler;
        EventCallback<ChangeEvent<string>> csvSelectionChangedHandler;
        readonly List<string> csvChoices = new();
        readonly Dictionary<string, string> valueToDisplay = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, string> displayToValue = new(StringComparer.OrdinalIgnoreCase);

        void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            if (loader == null)
            {
                loader = FindObjectOfType<MemoryTestGlbLoader>();
            }

            EnsureAssetsLoaded();
            BuildInterface();
        }

        void OnDisable()
        {
            if (memoryGraph != null)
            {
                memoryGraph.SamplesUpdated -= HandleMemorySamplesUpdated;
            }

            if (startButton != null)
            {
                startButton.clicked -= HandleStartClicked;
            }

            if (stopButton != null)
            {
                stopButton.clicked -= HandleStopClicked;
            }

            if (apiKeyField != null && apiKeyChangedHandler != null)
            {
                apiKeyField.UnregisterValueChangedCallback(apiKeyChangedHandler);
            }
            apiKeyChangedHandler = null;

            if (csvDropdown != null && csvSelectionChangedHandler != null)
            {
                csvDropdown.UnregisterValueChangedCallback(csvSelectionChangedHandler);
            }
            csvSelectionChangedHandler = null;
        }

        void Update()
        {
            if (loader == null)
            {
                return;
            }

            SyncCsvDropdownSelection();
#if UNITY_WEBGL && !UNITY_EDITOR
            if (apiKeyField != null)
            {
                apiKeyField.visible = false;
            }
#else
            UpdateApiKeyField();
#endif
            statusLabel.text = $"Status: {BuildStatusLabel()}";
            progressLabel.text = $"Progress: {loader.loadedFiles}/{loader.totalFiles}";

            UpdateButtonStates();
        }

        void EnsureAssetsLoaded()
        {
            // Resources path: Runtime/Resources/UI/MemoryTestGlbLoaderUI
            layout = Resources.Load<VisualTreeAsset>("UI/MemoryTestGlbLoaderUI");
            styleSheet = Resources.Load<StyleSheet>("UI/MemoryTestGlbLoaderUI");
        }

        void BuildInterface()
        {
            if (layout != null)
            {
                uiDocument.visualTreeAsset = layout;
            }

            VisualElement root = uiDocument.rootVisualElement;

            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            startButton = root.Q<Button>("start-button");
            stopButton = root.Q<Button>("stop-button");
            statusLabel = root.Q<Label>("status-label");
            progressLabel = root.Q<Label>("progress-label");
            csvDropdown = root.Q<DropdownField>("csv-dropdown");
            apiKeyField = root.Q<TextField>("api-key-field");
            heapLabel = root.Q<Label>("heap-label");
            gcHeapLabel = root.Q<Label>("gc-label");
            memoryGraph = root.Q<MemoryStatsGraph>("memory-graph");

            if (startButton != null)
            {
                startButton.clicked += HandleStartClicked;
            }

            if (stopButton != null)
            {
                stopButton.clicked += HandleStopClicked;
            }

            if (memoryGraph != null)
            {
                memoryGraph.SamplesUpdated += HandleMemorySamplesUpdated;
                HandleMemorySamplesUpdated(memoryGraph);
            }

            SetupCsvDropdown();

#if UNITY_WEBGL && !UNITY_EDITOR
            if (apiKeyField != null)
            {
                apiKeyField.visible = false;
            }
#else
            if (apiKeyField != null)
            {
                apiKeyChangedHandler = evt =>
                {
                    if (loader != null)
                    {
                        loader.GoogleApiKey = evt.newValue?.Trim();
                    }
                };
                apiKeyField.RegisterValueChangedCallback(apiKeyChangedHandler);
                UpdateApiKeyField();
            }
#endif

            Update();
        }

        void SetupCsvDropdown()
        {
            if (csvDropdown == null)
            {
                return;
            }

            RefreshCsvChoices();
            csvSelectionChangedHandler = evt =>
            {
                if (loader != null && !string.IsNullOrEmpty(evt.newValue))
                {
                    loader.CsvFileName = GetValueFromDisplay(evt.newValue);
                }
            };
            csvDropdown.RegisterValueChangedCallback(csvSelectionChangedHandler);
            csvDropdown.tooltip = "Select capture CSV";
            string initial = GetCurrentCsvSelection();
            string initialDisplay = valueToDisplay.TryGetValue(initial, out var mappedInitial)
                ? mappedInitial
                : FormatDisplayName(initial);
            csvDropdown.SetValueWithoutNotify(initialDisplay);
            if (loader != null && string.IsNullOrEmpty(loader.CsvFileName) && !string.IsNullOrEmpty(initial))
            {
                loader.CsvFileName = initial;
            }
        }

        void UpdateApiKeyField()
        {
            if (apiKeyField == null || loader == null)
            {
                return;
            }

            apiKeyField.SetValueWithoutNotify(loader.GoogleApiKey ?? string.Empty);
        }

        void RefreshCsvChoices()
        {
            csvChoices.Clear();
            valueToDisplay.Clear();
            displayToValue.Clear();

            HashSet<string> unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void TryAdd(string candidate)
            {
                string sanitized = MemoryTestGlbLoader.SanitizeCsvFileStem(candidate);
                if (string.IsNullOrEmpty(sanitized) || !unique.Add(sanitized))
                {
                    return;
                }

                csvChoices.Add(sanitized);
            }

            foreach (var asset in Resources.LoadAll<TextAsset>(MemoryTestGlbLoader.TestDataResourceRoot))
            {
                if (asset != null)
                {
                    TryAdd(asset.name);
                }
            }

#if UNITY_EDITOR
            string directory = MemoryTestGlbLoader.GetAbsoluteTestDataDirectory();
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                foreach (string file in Directory.GetFiles(directory, "*.csv", SearchOption.TopDirectoryOnly))
                {
                    TryAdd(Path.GetFileNameWithoutExtension(file));
                }
            }
#endif

            csvChoices.Sort(StringComparer.OrdinalIgnoreCase);

            List<string> displays = new List<string>(csvChoices.Count);
            foreach (string value in csvChoices)
            {
                string display = FormatDisplayName(value);
                valueToDisplay[value] = display;
                displayToValue[display] = value;
                displays.Add(display);
            }

            if (csvDropdown != null)
            {
                csvDropdown.choices = displays;
            }
        }

        string GetCurrentCsvSelection()
        {
            string current = loader != null ? loader.CsvFileName : null;
            string sanitized = MemoryTestGlbLoader.SanitizeCsvFileStem(current);

            bool hasCurrent = !string.IsNullOrEmpty(sanitized) &&
                csvChoices.Exists(choice => string.Equals(choice, sanitized, StringComparison.OrdinalIgnoreCase));

            if (!hasCurrent)
            {
                sanitized = csvChoices.Count > 0 ? csvChoices[0] : string.Empty;
                if (loader != null && !string.IsNullOrEmpty(sanitized))
                {
                    loader.CsvFileName = sanitized;
                }
            }

            return sanitized;
        }

        void SyncCsvDropdownSelection()
        {
            if (csvDropdown == null)
            {
                return;
            }

            string desired = GetCurrentCsvSelection();
            if (string.IsNullOrEmpty(desired))
            {
                return;
            }

            string display = valueToDisplay.TryGetValue(desired, out var mapped)
                ? mapped
                : FormatDisplayName(desired);

            if (csvDropdown.value != display)
            {
                csvDropdown.SetValueWithoutNotify(display);
            }
        }

        string FormatDisplayName(string value)
        {
            string sanitized = MemoryTestGlbLoader.SanitizeCsvFileStem(value);
            if (string.IsNullOrEmpty(sanitized))
            {
                return string.Empty;
            }

            return sanitized.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                ? sanitized
                : sanitized + ".csv";
        }

        string GetValueFromDisplay(string display)
        {
            if (string.IsNullOrEmpty(display))
            {
                return string.Empty;
            }

            if (displayToValue.TryGetValue(display, out var value))
            {
                return value;
            }

            return MemoryTestGlbLoader.SanitizeCsvFileStem(display);
        }

        string BuildStatusLabel()
        {
            if (loader == null)
            {
                return "Unknown";
            }

            if (loader.isLoading)
            {
                return "Loading…";
            }

            if (loader.IsAutomatedTestRunning)
            {
                return "Testing";
            }

            if (loader.HasActiveContent)
            {
                return "Loaded";
            }

            return "Idle";
        }

        void UpdateButtonStates()
        {
            if (loader == null)
            {
                return;
            }

            if (startButton != null)
            {
                startButton.SetEnabled(!loader.isLoading && !loader.IsAutomatedTestRunning);
            }

            if (stopButton != null)
            {
                bool canStop = loader.IsAutomatedTestRunning || loader.HasActiveContent || loader.isLoading;
                stopButton.SetEnabled(canStop);
            }
        }

        void HandleStartClicked()
        {
            if (loader == null)
            {
                return;
            }

            loader.StartAutomatedTest();
        }

        void HandleStopClicked()
        {
            if (loader == null)
            {
                return;
            }

            if (loader.IsAutomatedTestRunning)
            {
                loader.StopAutomatedTest();
            }
            else if (loader.isLoading)
            {
                loader.CancelLoading();
            }
            else
            {
                loader.ClearScene();
            }
        }

        void HandleMemorySamplesUpdated(MemoryStatsGraph graph)
        {
            if (heapLabel != null)
            {
                heapLabel.text = $"Heap size: {graph.LatestHeapSizeMB:0.0} MB";
            }
            if (gcHeapLabel != null)
            {
                gcHeapLabel.text = $"GC: {graph.LatestGcHeapMB:0.0} MB";
            }
        }
    }
}
