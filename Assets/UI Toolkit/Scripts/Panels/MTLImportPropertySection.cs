using System.Collections.Generic;
using Netherlands3D.Functionalities.OBJImporter;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(OBJPropertyData))]
    public partial class MTLImportPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private enum ViewState
        {
            Import,
            Error
        }
        
        private FileOpen fileOpen;
        
        private ColorPropertyData stylingPropertyData;
        private OBJPropertyData objPropertyData;

        private ErrorPanelContent errorPanel;
        private VisualElement defaultImportPanel;

        private Button importButton;

        public MTLImportPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            importButton = this.Q<Button>();
            importButton.RegisterCallback<ClickEvent>(StartImport);
            fileOpen = ServiceLocator.GetService<FileOpen>();
            fileOpen.onFilesSelected.AddListener(ImportMtl);

            defaultImportPanel = this.Q<VisualElement>("ImportMTLSection");
            errorPanel = this.Q<ErrorPanelContent>();

            errorPanel.OnHide.AddListener(() => defaultImportPanel.RemoveFromClassList(UtilityClassConstants.HIDDEN));
            
            SetViewState(ViewState.Import);
            
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            fileOpen.onFilesSelected.RemoveListener(ImportMtl);
            objPropertyData.MtlImportSuccess.RemoveListener(OnMTLImportCompleted);
        }

        private void StartImport(ClickEvent evt)
        {
            fileOpen.OpenFile("mtl");
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            objPropertyData = properties.Get<OBJPropertyData>();
            stylingPropertyData = properties.GetDefaultStylingPropertyData<ColorPropertyData>();
            objPropertyData.MtlImportSuccess.AddListener(OnMTLImportCompleted);
        }

        public void ImportMtl(string path)
        {
            path = path.TrimEnd(',');

            if (!path.EndsWith(".mtl"))
                return;

            // When importing an MTL - we want to reset the coloring of the object
            stylingPropertyData.SetDefaultSymbolizerColor(null);

            SetMtlPathInPropertyData(path);
        }

        private void SetMtlPathInPropertyData(string fullPath)
        {
            objPropertyData.MtlFile = AssetUriFactory.CreateProjectAssetUri(fullPath);
        }
        
        private void SetViewState(ViewState state)
        {
            if (state == ViewState.Error)
                errorPanel.Show();
            else
                errorPanel.Hide();
            
            defaultImportPanel.EnableInClassList(UtilityClassConstants.HIDDEN, state == ViewState.Error);
        }

        private void OnMTLImportCompleted(bool success)
        {
            SetViewState(success ? ViewState.Import : ViewState.Error);
        }
    }
}