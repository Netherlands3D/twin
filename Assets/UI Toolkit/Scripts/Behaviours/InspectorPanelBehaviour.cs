using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Credentials;
using Netherlands3D.Twin.Configuration;
using Netherlands3D.Twin.Tools;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Behaviours
{
    [RequireComponent(typeof(UIDocument), typeof(ICredentialHandler))]
    public class InspectorPanelBehaviour : MonoBehaviour
    {
        private UIDocument appDocument;
        [SerializeField] private AssetLibrary.AssetLibrary assetLibrary;
    
        private VisualElement root;
        private VisualElement Root => root ??= appDocument?.rootVisualElement;

        private InspectorPanel inspectorPanel;
        private InspectorPanel InspectorPanel => inspectorPanel ??= Root?.Q<InspectorPanel>();

        private ImportAssetPanel importAssetPanel;
        private ImportAssetPanel ImportAssetPanel => importAssetPanel ??= panels.OfType<ImportAssetPanel>().FirstOrDefault();

        private readonly HashSet<BaseInspectorContentPanel> panels = new();
        private BaseInspectorContentPanel activePanel;
        
        private ToolbarMain toolbarMain;
        private ToolbarMain ToolbarMain => toolbarMain ??= Root?.Q<ToolbarMain>();
        
        private ICredentialHandler credentialHandler;

        [Header("Tools")]
        [SerializeField] private Tool AssetLibrary;
        [SerializeField] private Tool AssetImport;
        [SerializeField] private Tool Layer;
        [SerializeField] private Tool SearchTool;
        [SerializeField] private Tool SunPosition;
        [SerializeField] private Tool DownloadTile;
        [SerializeField] private Tool OpenProject;
        [SerializeField] private Tool SaveProject;
        [SerializeField] private Tool SettingsTool;
        [SerializeField] private Tool HelpTool;

        [Header("External Windows")]
        [SerializeField] private GameObject Search;
        [SerializeField] private ScriptableObject SettingsWindow;
        [SerializeField] private string HelpUrl;

        /// <summary>
        /// Pairs a Tool with its inspector open-action and a cached UnityAction delegate
        /// for symmetrical subscribe/unsubscribe without allocating new lambdas each cycle.
        /// </summary>
        private sealed class ToolEntry
        {
            public Tool Tool { get; }
            public Action OnOpen { get; }
            public UnityAction OpenListener { get; }

            public ToolEntry(Tool tool, Action onOpen, Action<ToolEntry> dispatchOpen)
            {
                Tool = tool;
                OnOpen = onOpen;
                OpenListener = () => dispatchOpen(this);
            }
        }

        /// <summary>
        /// Small repository responsible for storing and iterating registered tools.
        /// InspectorPanelBehaviour provides the actual tool-open behavior; this class
        /// only manages the registrations and list-based queries/operations.
        /// </summary>
        private sealed class ToolRepository
        {
            private readonly List<ToolEntry> entries = new();
            private readonly Action<ToolEntry> dispatchOpen;

            public ToolRepository(Action<ToolEntry> dispatchOpen)
            {
                this.dispatchOpen = dispatchOpen;
            }

            public void Add(Tool tool, Action onOpen)
            {
                if (tool == null) return;
                entries.Add(new ToolEntry(tool, onOpen, dispatchOpen));
            }

            public void SubscribeAll(UnityAction onToolClosed)
            {
                foreach (var entry in entries)
                {
                    entry.Tool.onOpen.AddListener(entry.OpenListener);
                    entry.Tool.onClose.AddListener(onToolClosed);
                }
            }

            public void UnsubscribeAll(UnityAction onToolClosed)
            {
                foreach (var entry in entries)
                {
                    entry.Tool.onOpen.RemoveListener(entry.OpenListener);
                    entry.Tool.onClose.RemoveListener(onToolClosed);
                }
            }

            public void CloseAllExcept(ToolEntry activeEntry)
            {
                foreach (var entry in entries)
                {
                    if (entry.Tool != activeEntry.Tool)
                        entry.Tool.CloseInspector();
                }
            }

            public bool HasOpenTools()
            {
                return entries.Any(entry => entry.Tool != null && entry.Tool.Open);
            }
        }

        private ToolRepository toolRepository;

        private void Awake()
        {
            appDocument = GetComponent<UIDocument>();
            credentialHandler = GetComponent<ICredentialHandler>();
            RegisterPanel<AssetLibraryPanel>(assetLibrary);
            RegisterPanel<ImportAssetPanel>();
            
            InspectorPanel.Close();
            
            ImportAssetPanel.SetCredentialHandler(credentialHandler);

            toolRepository = new ToolRepository(OnAnyToolOpened);

            // Register every tool once with its inspector open-action.
            // External tools (not managed by the InspectorPanel) call CloseInspectorPanels.
            RegisterTool(AssetLibrary, OpenAssetLibraryPanel);
            RegisterTool(AssetImport, OpenAssetImportPanel);
            RegisterTool(Layer, CloseInspectorPanels);
            RegisterTool(SearchTool, OpenSearchTool);
            RegisterTool(SunPosition, CloseInspectorPanels);
            RegisterTool(DownloadTile, CloseInspectorPanels);
            RegisterTool(OpenProject, CloseInspectorPanels);
            RegisterTool(SaveProject, CloseInspectorPanels);
            RegisterTool(SettingsTool, OpenSettingsTool);
            RegisterTool(HelpTool, OpenHelpTool);
        }

        private void RegisterTool(Tool tool, Action onOpen)
        {
            toolRepository.Add(tool, onOpen);
        }

        private void OnEnable()
        {
            InspectorPanel.Toolbar.OnAddLayerToggled += OnAddLayerToggled;
            InspectorPanel.Toolbar.OnOpenLibraryToggled += OnOpenLibraryToggled;
            InspectorPanel.InspectorHeaderCloseButton.clicked += Close;
            ImportAssetPanel.OpenAssetLibrary += OnOpenAssetLibraryClicked;
            ImportAssetPanel.importSucceeded.AddListener(OnImportSucceeded);

            toolRepository.SubscribeAll(OnToolClosed);
        }

        private void OnDisable()
        {
            InspectorPanel.Toolbar.OnAddLayerToggled -= OnAddLayerToggled;
            InspectorPanel.Toolbar.OnOpenLibraryToggled -= OnOpenLibraryToggled;
            InspectorPanel.InspectorHeaderCloseButton.clicked -= Close;
            ImportAssetPanel.OpenAssetLibrary -= OnOpenAssetLibraryClicked;
            ImportAssetPanel.importSucceeded.RemoveListener(OnImportSucceeded);

            toolRepository.UnsubscribeAll(OnToolClosed);
        }

        public void Open()
        {
            InspectorPanel.Open();
        }

        public void Close()
        {
            // TODO: Remove as soon as search is implemented as a panel
            Search.SetActive(false);
            
            ToolbarMain.ClearWithoutNotify();
            InspectorPanel.Toolbar.ToggleButtonsOffWithoutNotify();
            InspectorPanel.Close();
        }

        // TODO: Shouldn't this be in the InspectorPanel component?
        private BaseInspectorContentPanel RegisterPanel<T>(params object[] args) where T : BaseInspectorContentPanel
        {
            var panel = (T)Activator.CreateInstance(typeof(T), args);
            panels.Add(panel);

            InspectorPanel.Content.Add(panel);
            panel.Hide();

            return panel;
        }

        private void OnAnyToolOpened(ToolEntry entry)
        {
            toolRepository.CloseAllExcept(entry);
            entry.OnOpen?.Invoke();
        }

        private void OpenAssetLibraryPanel()
        {
            CloseInspectorPanels();
            ShowPanel<AssetLibraryPanel>();
        }

        private void OpenAssetImportPanel()
        {
            CloseInspectorPanels();
            ShowPanel<ImportAssetPanel>();
        }

        private void OpenSearchTool()
        {
            CloseInspectorPanels();
            Search.SetActive(true);
        }

        private void OpenSettingsTool()
        {
            CloseInspectorPanels();
            ((IWindow)SettingsWindow).Open();
            SettingsTool?.CloseInspector();
        }

        private void OpenHelpTool()
        {
            Application.OpenURL(HelpUrl);
            HelpTool?.CloseInspector();
        }

        // External tools close the UI Toolkit inspector without clearing the main toolbar selection.

        private void ShowPanel<T>() where T : BaseInspectorContentPanel
        {
            // only one panel can be open at a time
            HidePanel();
            Open();
            activePanel = GetPanel<T>();
            InspectorPanel.HeaderText = activePanel.GetTitle();
            InspectorPanel.ToolbarStyle = activePanel.ToolbarStyle;
            activePanel.Show();
        }

        private T GetPanel<T>() where T : BaseInspectorContentPanel
        {
            return panels.OfType<T>().FirstOrDefault();
        }

        private void HidePanel()
        {
            activePanel?.Hide();
            activePanel = null;
        }

        private void CloseInspectorPanels()
        {
            ((IWindow)SettingsWindow).Close();
            Search.SetActive(false);
            HidePanel();
            InspectorPanel.Toolbar.ToggleButtonsOffWithoutNotify();
            InspectorPanel.Close();
        }

        private void OnAddLayerToggled(ChangeEvent<bool> evt)
        {
            if (evt.newValue) AssetImport?.OpenInspector();
            else AssetImport?.CloseInspector();
        }

        private void OnOpenLibraryToggled(ChangeEvent<bool> evt)
        {
            if (evt.newValue) AssetLibrary?.OpenInspector();
            else AssetLibrary?.CloseInspector();
        }

        private void OnOpenAssetLibraryClicked()
        {
            AssetLibrary?.OpenInspector();
        }

        private void OnToolClosed()
        {
            if (!toolRepository.HasOpenTools())
                Close();
        }

        private void OnImportSucceeded()
        {
            InspectorPanel.Toolbar.AddLayer.SetValueWithoutNotify(false);
            Close();
        }
    }
}
