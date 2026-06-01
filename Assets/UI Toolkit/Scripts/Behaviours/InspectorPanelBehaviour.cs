using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Credentials;
using Netherlands3D.Events;
using Netherlands3D.Services;
using Netherlands3D.Twin;
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
    public class InspectorPanelBehaviour : MonoBehaviour
    {
        [SerializeField] private AssetLibrary.AssetLibrary assetLibrary;
        [SerializeField] private LocationSearchBehaviour locationSearchBehaviour;

        private InspectorPanel inspectorPanel;
        
        private Tool activeToolWithPanel;
        private BaseInspectorContentPanel activePanel;
        private ToolService tools;

        [SerializeField] private TriggerEvent OnDrawNewGrid;
        [SerializeField] private TriggerEvent OnGridConfirmed;
        
        [Header("External Windows")]
        [SerializeField] private ScriptableObject SettingsWindow;
        [SerializeField] private string HelpUrl;


        private void Awake()
        {
            tools = ServiceLocator.GetService<ToolService>();
            
            // RegisterPanel<AssetLibraryPanel>(assetLibrary); //todo: set assetLibrary in panel
            // RegisterPanel<ImportAssetPanel>();
            // RegisterPanel<InspectorPolygonGridPanel>();
            
            // RegisterPanel<LocationSearchPanel>();
            // locationSearchBehaviour?.Initialize(GetPanel<LocationSearchPanel>()); //todo: figure out what this does

            
            inspectorPanel = App.UIRoot.Root.Q<InspectorPanel>();
            inspectorPanel.Close();
            
            // ImportAssetPanel.SetCredentialHandler(credentialHandler); //todo: set credential handler in importAssetPanel
        }

        private void Start()
        {
            PolygonSelectionService polygonSelectionService = ServiceLocator.GetService<PolygonSelectionService>();
            inspectorPanel.OnShow += polygonSelectionService.EnablePolygonSelection;
            inspectorPanel.OnHide += polygonSelectionService.DisablePolygonSelection;
        }

        private void OnEnable()
        {
            foreach (var toolWithPanel in tools.GetAllToolsWithPanel())
            {
                toolWithPanel.onOpen.AddListener(() => OnToolWithPanelOpen(toolWithPanel)); //todo: make this not a lambda function so we can unsubscribe
                toolWithPanel.onClose.AddListener(Close);
            }
            
            //tools.onPreNotifyAny.AddListener(CloseInspectorPanels);
            // tools.AddOpenListener(ToolType.Settings, ((IWindow)SettingsWindow).Open);
            // tools.AddOpenListener(ToolType.Help, OpenHelp);
            
            
            // InspectorPanel.Toolbar.OnAddLayerToggled += OnAddLayerToggled;
            // InspectorPanel.Toolbar.OnOpenLibraryToggled += OnOpenLibraryToggled;
            inspectorPanel.InspectorHeaderCloseButton.clicked += Close;
            // ImportAssetPanel.OpenAssetLibrary += tools.GetTool(ToolType.AssetLibrary).OpenInspector;
            // ImportAssetPanel.importSucceeded.AddListener(OnImportSucceeded);
            
            // OnDrawNewGrid.AddListenerStarted(OpenPolgyonGridPanel); //todo polygon grid should become a tool
            //
            // PolygonGridPanel.OnConfirmSelection.AddListener(OnGridConfirmed.InvokeStarted);
            //TODO ongridconfirmed -> open layerpanel and close the gridpanel (if its not automatically happening)


           
            
           // toolRepository.SubscribeAll(OnToolClosed);
        }

        private void OnToolWithPanelOpen(Tool toolWithPanel)
        {
            activeToolWithPanel?.CloseInspector();
            activePanel?.OnHide.RemoveListener(Close);
            activeToolWithPanel = toolWithPanel;
            
            Open();
            
            activePanel = CreatePanel(toolWithPanel.PanelType, toolWithPanel.PanelArgs);
            inspectorPanel.HeaderText = activePanel.Title;
            inspectorPanel.ToolbarStyle = activePanel.ToolbarStyle;
            activePanel.OnHide.AddListener(Close);
        }

        private void OnDisable()
        {
            // tools.onPreNotifyAny.RemoveListener(CloseInspectorPanels);
            tools.RemoveOpenListener(ToolType.Settings, ((IWindow)SettingsWindow).Open);
            tools.RemoveOpenListener(ToolType.Help, OpenHelp);
            // tools.RemoveOpenListener(ToolType.AssetLibrary, ShowPanel<AssetLibraryPanel>);
            // tools.RemoveOpenListener(ToolType.AssetImport, ShowPanel<ImportAssetPanel>);
            
            // InspectorPanel.Toolbar.OnAddLayerToggled -= OnAddLayerToggled;
            // InspectorPanel.Toolbar.OnOpenLibraryToggled -= OnOpenLibraryToggled;
            inspectorPanel.InspectorHeaderCloseButton.clicked -= Close;
            // ImportAssetPanel.OpenAssetLibrary -= tools.GetTool(ToolType.AssetLibrary).OpenInspector;
            // ImportAssetPanel.importSucceeded.RemoveListener(OnImportSucceeded);
            
            // OnDrawNewGrid.RemoveListenerStarted(OpenPolgyonGridPanel);
            //
            // PolygonGridPanel.OnConfirmSelection.RemoveListener(OnGridConfirmed.InvokeStarted);

           // toolRepository.UnsubscribeAll(OnToolClosed);
        }

        public void Open()
        {
            inspectorPanel.Open();
        }

        public void Close()
        {
            // tools.ClearWithoutNotify.Invoke();
            // InspectorPanel.Toolbar.ToggleButtonsOffWithoutNotify(); //todo: this should be done through the tool that is closed and the associated button that listens to the tool.Onclose.
            // HidePanel();
            inspectorPanel.Content.Clear();
            inspectorPanel.Close();
        }

        // TODO: Shouldn't this be in the InspectorPanel component?
        private BaseInspectorContentPanel CreatePanel(Type panelType, params object[] args)
        {
            if (!panelType.IsSubclassOf(typeof(BaseInspectorContentPanel)))
                throw new ArgumentException("panelType must derive from BaseInspectorContentPanel");
                
            var panel = Activator.CreateInstance(panelType, args) as BaseInspectorContentPanel;
            // panels.Add(panel);

            inspectorPanel.Content.Add(panel);
            // panel.Hide();

            return panel;
        }

        private void OpenHelp() => Application.OpenURL(HelpUrl);
        
        // private void CloseInspectorPanels(ToolType toolType)
        // {
        //     ((IWindow)SettingsWindow).Close();
        //     HidePanel();
        //     InspectorPanel.Toolbar.ToggleButtonsOffWithoutNotify();
        //     InspectorPanel.Close();
        // }

        // private void OnImportSucceeded()
        // {
        //     InspectorPanel.Toolbar.AddLayer.SetValueWithoutNotify(false);
        //     Close();
        // }
    }
}
