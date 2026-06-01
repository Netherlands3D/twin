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
        private ToolService toolService;

        [SerializeField] private TriggerEvent OnDrawNewGrid;
        [SerializeField] private TriggerEvent OnGridConfirmed;
        
        [Header("External Windows")]
        [SerializeField] private ScriptableObject SettingsWindow;
        [SerializeField] private string HelpUrl;

        private HamburgerMenu hamburgerMenu;
        private ToolbarMain toolbarMain;
        private Dictionary<Tool, UnityAction> toolListeners = new();
        
        private void Awake()
        {
            toolService = ServiceLocator.GetService<ToolService>();
          
            // locationSearchBehaviour?.Initialize(GetPanel<LocationSearchPanel>()); //todo: figure out what this does
            
            inspectorPanel = App.UIRoot.Root.Q<InspectorPanel>();
            inspectorPanel.Close();
            
            hamburgerMenu = App.UIRoot.Root.Q<HamburgerMenu>();
            toolbarMain = App.UIRoot.Root.Q<ToolbarMain>();
            
            toolService.AnyToolOpened.AddListener(OnAnyToolOpened);
            foreach (var toolWithPanel in toolService.GetAllToolsWithPanel())
            {
                var tool = toolWithPanel;
                UnityAction listener = () => OnToolWithPanelOpen(tool);
                toolListeners[tool] = listener;
            }
        }

        private void Start()
        {
            PolygonSelectionService polygonSelectionService = ServiceLocator.GetService<PolygonSelectionService>();
            inspectorPanel.OnShow += polygonSelectionService.EnablePolygonSelection;
            inspectorPanel.OnHide += polygonSelectionService.DisablePolygonSelection;
            
            toolService.AnyToolClosed.AddListener(toolbarMain.UpdateState);
            toolService.AnyToolOpened.AddListener(toolbarMain.UpdateState);
        }

        private void OnEnable()
        {
            foreach (var toolWithPanel in toolService.GetAllToolsWithPanel())
            {
                toolWithPanel.onOpen.AddListener(toolListeners[toolWithPanel]);
                toolWithPanel.onClose.AddListener(Close);
            }
            
            toolService.GetTool(ToolType.Settings).onOpen.AddListener(((IWindow)SettingsWindow).Open);
            toolService.GetTool(ToolType.Help).onOpen.AddListener(OpenHelp);
           
            // InspectorPanel.Toolbar.OnAddLayerToggled += OnAddLayerToggled;
            // InspectorPanel.Toolbar.OnOpenLibraryToggled += OnOpenLibraryToggled;
            inspectorPanel.InspectorHeaderCloseButton.clicked += Close;
            
            // OnDrawNewGrid.AddListenerStarted(OpenPolgyonGridPanel); //todo polygon grid should become a tool
            // PolygonGridPanel.OnConfirmSelection.AddListener(OnGridConfirmed.InvokeStarted);
            //TODO ongridconfirmed -> open layerpanel and close the gridpanel (if its not automatically happening)
        }
        
        private void OnDisable()
        {
            foreach (var toolWithPanel in toolService.GetAllToolsWithPanel())
            {
                toolWithPanel.onOpen.RemoveListener(toolListeners[toolWithPanel]);
                toolWithPanel.onClose.RemoveListener(Close);
            }
            
            toolService.GetTool(ToolType.Settings).onOpen.RemoveListener(((IWindow)SettingsWindow).Open);
            toolService.GetTool(ToolType.Help).onOpen.RemoveListener(OpenHelp);
            
            // InspectorPanel.Toolbar.OnAddLayerToggled -= OnAddLayerToggled;
            // InspectorPanel.Toolbar.OnOpenLibraryToggled -= OnOpenLibraryToggled;
            inspectorPanel.InspectorHeaderCloseButton.clicked -= Close;
            toolService.AnyToolOpened.RemoveListener(OnAnyToolOpened);
            
            // OnDrawNewGrid.RemoveListenerStarted(OpenPolgyonGridPanel);
            // PolygonGridPanel.OnConfirmSelection.RemoveListener(OnGridConfirmed.InvokeStarted);
        }
        
        private void OnAnyToolOpened()
        {
            hamburgerMenu.Close();
        }

        private void OnToolWithPanelOpen(Tool toolWithPanel)
        {
            activeToolWithPanel?.Close();
            activePanel?.OnHide.RemoveListener(Close);
            activeToolWithPanel = toolWithPanel;
            
            Open();
            
            activePanel = CreatePanel(toolWithPanel.PanelType, toolWithPanel.PanelArgs);
            inspectorPanel.HeaderText = activePanel.Title;
            inspectorPanel.ToolbarStyle = activePanel.ToolbarStyle;
            activePanel.OnHide.AddListener(Close);
        }

        public void Open()
        {
            inspectorPanel.Open();
        }

        public void Close()
        {
            activeToolWithPanel = null;
            activePanel = null;
            inspectorPanel.ClearContent();
            inspectorPanel.Close();
        }
      
        private BaseInspectorContentPanel CreatePanel(Type panelType, params object[] args)
        {
            if (!panelType.IsSubclassOf(typeof(BaseInspectorContentPanel)))
                throw new ArgumentException("panelType must derive from BaseInspectorContentPanel");
                
            var panel = Activator.CreateInstance(panelType, args) as BaseInspectorContentPanel;
            inspectorPanel.AddContent(panel);
            return panel;
        }

        private void OpenHelp() => Application.OpenURL(HelpUrl);
    }
}
