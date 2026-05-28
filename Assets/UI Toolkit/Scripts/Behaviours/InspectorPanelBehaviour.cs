using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Credentials;
using Netherlands3D.Events;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
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
        [SerializeField] private LocationSearchBehaviour locationSearchBehaviour;

        private VisualElement root;
        private VisualElement Root => root ??= appDocument?.rootVisualElement;

        private InspectorPanel inspectorPanel;
        private InspectorPanel InspectorPanel => inspectorPanel ??= Root?.Q<InspectorPanel>();

        private ImportAssetPanel importAssetPanel;
        private ImportAssetPanel ImportAssetPanel => importAssetPanel ??= panels.OfType<ImportAssetPanel>().FirstOrDefault();
        
        private InspectorPolygonGridPanel polygonGridPanel;
        private InspectorPolygonGridPanel PolygonGridPanel => polygonGridPanel ??= panels.OfType<InspectorPolygonGridPanel>().FirstOrDefault();

        private readonly HashSet<BaseInspectorContentPanel> panels = new();
        private BaseInspectorContentPanel activePanel;
        
        private ToolbarMain toolbarMain;
        private ToolbarMain ToolbarMain => toolbarMain ??= Root?.Q<ToolbarMain>();
        
        private ICredentialHandler credentialHandler;
        private ToolService tools;

        [SerializeField] private TriggerEvent OnDrawNewGrid;
        [SerializeField] private TriggerEvent OnGridConfirmed;
     
      
        //
        // [Header("Tools")]
        // [SerializeField] private Tool AssetLibrary;
        // [SerializeField] private Tool AssetImport;
        // [SerializeField] private Tool Layer;
        // [SerializeField] private Tool SearchTool;
        // [SerializeField] private Tool SunPosition;
        // [SerializeField] private Tool DownloadTile;
        // [SerializeField] private Tool OpenProject;
        // [SerializeField] private Tool SaveProject;
        // [SerializeField] private Tool SettingsTool;
        // [SerializeField] private Tool HelpTool;

        [Header("External Windows")]
        [SerializeField] private ScriptableObject SettingsWindow;
        [SerializeField] private string HelpUrl;

        // /// <summary>
        // /// Pairs a Tool with its inspector open-action and a cached UnityAction delegate
        // /// for symmetrical subscribe/unsubscribe without allocating new lambdas each cycle.
        // /// </summary>
        // private sealed class ToolEntry
        // {
        //     public Tool Tool { get; }
        //     public Action OnOpen { get; }
        //     public UnityAction OpenListener { get; }
        //
        //     public ToolEntry(Tool tool, Action onOpen, Action<ToolEntry> dispatchOpen)
        //     {
        //         Tool = tool;
        //         OnOpen = onOpen;
        //         OpenListener = () => dispatchOpen(this);
        //     }
        // }

        // /// <summary>
        // /// Small repository responsible for storing and iterating registered tools.
        // /// InspectorPanelBehaviour provides the actual tool-open behavior; this class
        // /// only manages the registrations and list-based queries/operations.
        // /// </summary>
        // private sealed class ToolRepository
        // {
        //     private readonly List<ToolEntry> entries = new();
        //     private readonly Action<ToolEntry> dispatchOpen;
        //
        //     public ToolRepository(Action<ToolEntry> dispatchOpen)
        //     {
        //         this.dispatchOpen = dispatchOpen;
        //     }
        //
        //     public void Add(Tool tool, Action onOpen)
        //     {
        //         if (tool == null) return;
        //         entries.Add(new ToolEntry(tool, onOpen, dispatchOpen));
        //     }
        //
        //     public void SubscribeAll(UnityAction onToolClosed)
        //     {
        //         foreach (var entry in entries)
        //         {
        //             entry.Tool.onOpen.AddListener(entry.OpenListener);
        //             entry.Tool.onClose.AddListener(onToolClosed);
        //         }
        //     }
        //
        //     public void UnsubscribeAll(UnityAction onToolClosed)
        //     {
        //         foreach (var entry in entries)
        //         {
        //             entry.Tool.onOpen.RemoveListener(entry.OpenListener);
        //             entry.Tool.onClose.RemoveListener(onToolClosed);
        //         }
        //     }
        //
        //     public void CloseAllExcept(ToolEntry activeEntry)
        //     {
        //         foreach (var entry in entries)
        //         {
        //             if (entry.Tool != activeEntry.Tool)
        //                 entry.Tool.CloseInspector();
        //         }
        //     }
        //
        //     public bool HasOpenTools()
        //     {
        //         return entries.Any(entry => entry.Tool != null && entry.Tool.Open);
        //     }
        // }
        //
        // private ToolRepository toolRepository;

        private void Awake()
        {
            appDocument = GetComponent<UIDocument>();
            credentialHandler = GetComponent<ICredentialHandler>();
            tools = ServiceLocator.GetService<ToolService>();
            
            RegisterPanel<AssetLibraryPanel>(assetLibrary);
            RegisterPanel<ImportAssetPanel>();
            RegisterPanel<InspectorPolygonGridPanel>();
            
            RegisterPanel<LocationSearchPanel>();
            locationSearchBehaviour?.Initialize(GetPanel<LocationSearchPanel>());

            InspectorPanel.Close();
            
            ImportAssetPanel.SetCredentialHandler(credentialHandler);
            
          

            // // toolRepository = new ToolRepository(OnAnyToolOpened);
            //
            // // Register every tool once with its inspector open-action.
            // // External tools (not managed by the InspectorPanel) call CloseInspectorPanels.
            // RegisterTool(AssetLibrary, OpenAssetLibraryPanel);
            // RegisterTool(AssetImport, OpenAssetImportPanel);
            // RegisterTool(Layer, CloseInspectorPanels);
            // RegisterTool(SearchTool, OpenSearchTool);
            // RegisterTool(SunPosition, CloseInspectorPanels);
            // RegisterTool(DownloadTile, CloseInspectorPanels);
            // RegisterTool(OpenProject, CloseInspectorPanels);
            // RegisterTool(SaveProject, CloseInspectorPanels);
            // RegisterTool(SettingsTool, OpenSettingsTool);
            // RegisterTool(HelpTool, OpenHelpTool);
        }

        // private void RegisterTool(Tool tool, Action onOpen)
        // {
        //     toolRepository.Add(tool, onOpen);
        // }

        private void Start()
        {
           
            InspectorPanel.Initialize();
        }

        private void OnEnable()
        {
            tools.AddOpenListener(ToolType.Settings, OpenSettingsTool);
            tools.AddOpenListener(ToolType.Help, OpenHelpTool);
            
            
            // InspectorPanel.Toolbar.OnAddLayerToggled += OnAddLayerToggled;
            // InspectorPanel.Toolbar.OnOpenLibraryToggled += OnOpenLibraryToggled;
            InspectorPanel.InspectorHeaderCloseButton.clicked += Close;
            ImportAssetPanel.OpenAssetLibrary += tools.GetTool(ToolType.AssetLibrary).OpenInspector;
            ImportAssetPanel.importSucceeded.AddListener(OnImportSucceeded);
            
            OnDrawNewGrid.AddListenerStarted(OpenPolgyonGridPanel);
            
            PolygonGridPanel.OnConfirmSelection.AddListener(OnGridConfirmed.InvokeStarted);
            //TODO ongridconfirmed -> open layerpanel and close the gridpanel (if its not automatically happening)


           
            
           // toolRepository.SubscribeAll(OnToolClosed);
        }

        private void OnDisable()
        {
            tools.RemoveOpenListener(ToolType.Settings, OpenSettingsTool);
            tools.RemoveOpenListener(ToolType.Help, OpenHelpTool);
            
            // InspectorPanel.Toolbar.OnAddLayerToggled -= OnAddLayerToggled;
            // InspectorPanel.Toolbar.OnOpenLibraryToggled -= OnOpenLibraryToggled;
            InspectorPanel.InspectorHeaderCloseButton.clicked -= Close;
            ImportAssetPanel.OpenAssetLibrary -= tools.GetTool(ToolType.AssetLibrary).OpenInspector;
            ImportAssetPanel.importSucceeded.RemoveListener(OnImportSucceeded);
            
            OnDrawNewGrid.RemoveListenerStarted(OpenPolgyonGridPanel);
            
            PolygonGridPanel.OnConfirmSelection.RemoveListener(OnGridConfirmed.InvokeStarted);

           // toolRepository.UnsubscribeAll(OnToolClosed);
        }

        public void Open()
        {
            InspectorPanel.Open();
        }

        public void Close()
        {
            ToolbarMain.ClearWithoutNotify();
            InspectorPanel.Toolbar.ToggleButtonsOffWithoutNotify();
            HidePanel();
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

        public void ShowPanel<T>() where T : BaseInspectorContentPanel
        {
            // only one panel can be open at a time
            HidePanel();
            Open();
            activePanel = GetPanel<T>();
            InspectorPanel.HeaderText = activePanel.Title;
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

        // public void OpenAssetLibrary()
        // {
        //     ShowPanel<AssetLibraryPanel>();
        // }

        public void OpenPolgyonGridPanel()
        {
            ShowPanel<InspectorPolygonGridPanel>();
        }
        
        // private void OnAnyToolOpened(ToolEntry entry)
        // {
        //     toolRepository.CloseAllExcept(entry);
        //     entry.OnOpen?.Invoke();
        // }

        // private void OpenAssetLibraryPanel()
        // {
        //     CloseInspectorPanels();
        //     ShowPanel<AssetLibraryPanel>();
        // }
        //
        // private void OpenAssetImportPanel()
        // {
        //     CloseInspectorPanels();
        //     ShowPanel<ImportAssetPanel>();
        // }
        //
        // private void OpenSearchTool()
        // {
        //     CloseInspectorPanels();
        //     ShowPanel<LocationSearchPanel>();
        // }
        //
        private void OpenSettingsTool()
        {
            CloseInspectorPanels();
            ((IWindow)SettingsWindow).Open();
            //SettingsTool?.CloseInspector();
        }
       
        private void OpenHelpTool()
        {
            Application.OpenURL(HelpUrl);
            //HelpTool?.CloseInspector();
        }
        //
        //
        // public void ToggleImportAssetPanel()
        // {
        //     if (inspectorPanel.IsOpen())
        //     {
        //         HidePanel();
        //         //do not use Close here to avoid the toggle notification
        //         InspectorPanel.Close(); 
        //     }
        //     else
        //     {   
        //         Open();
        //         InspectorPanel.Toolbar.AddLayer.SetValueWithoutNotify(true);
        //         ShowPanel<ImportAssetPanel>();
        //     }
        // }
        
        private void CloseInspectorPanels()
        {
            ((IWindow)SettingsWindow).Close();
            HidePanel();
            InspectorPanel.Toolbar.ToggleButtonsOffWithoutNotify();
            InspectorPanel.Close();
        }

        // private void OnAddLayerToggled(ChangeEvent<bool> evt)
        // {
        //     if (evt.newValue) AssetImport?.OpenInspector();
        //     else AssetImport?.CloseInspector();
        // }
        //
        // private void OnOpenLibraryToggled(ChangeEvent<bool> evt)
        // {
        //     if (evt.newValue) AssetLibrary?.OpenInspector();
        //     else AssetLibrary?.CloseInspector();
        // }
        //
        // private void OnOpenAssetLibraryClicked()
        // {
        //     AssetLibrary?.OpenInspector();
        // }
        //
        // private void OnToolClosed()
        // {
        //     if (!toolRepository.HasOpenTools())
        //         Close();
        // }

        private void OnImportSucceeded()
        {
            InspectorPanel.Toolbar.AddLayer.SetValueWithoutNotify(false);
            Close();
        }
    }
}
