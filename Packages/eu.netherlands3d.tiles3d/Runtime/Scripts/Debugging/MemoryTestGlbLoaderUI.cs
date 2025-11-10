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
        Label csvLabel;
        TextField apiKeyField;
        Label allocatedLabel;
        Label reservedLabel;
        Label monoLabel;
        MemoryStatsGraph memoryGraph;
        EventCallback<ChangeEvent<string>> apiKeyChangedHandler;

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
        }

        void Update()
        {
            if (loader == null)
            {
                return;
            }

            UpdateCsvLabel();
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
            csvLabel = root.Q<Label>("csv-label");
            apiKeyField = root.Q<TextField>("api-key-field");
            allocatedLabel = root.Q<Label>("allocated-label");
            reservedLabel = root.Q<Label>("reserved-label");
            monoLabel = root.Q<Label>("mono-label");
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

        void UpdateCsvLabel()
        {
            if (csvLabel == null || loader == null)
            {
                return;
            }

            csvLabel.text = "CSV: Embedded test dataset";
            csvLabel.tooltip = loader.CsvSourceDescription;
        }

        void UpdateApiKeyField()
        {
            if (apiKeyField == null || loader == null)
            {
                return;
            }

            apiKeyField.SetValueWithoutNotify(loader.GoogleApiKey ?? string.Empty);
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
                bool canStop = loader.IsAutomatedTestRunning || loader.HasActiveContent;
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
            else if (!loader.isLoading)
            {
                loader.ClearScene();
            }
        }

        void HandleMemorySamplesUpdated(MemoryStatsGraph graph)
        {
            allocatedLabel.text = $"Allocated: {graph.LatestAllocatedMB:0.0} MB";
            reservedLabel.text = $"Reserved: {graph.LatestReservedMB:0.0} MB";
            monoLabel.text = $"Mono Used: {graph.LatestMonoUsedMB:0.0} MB";
        }
    }
}
