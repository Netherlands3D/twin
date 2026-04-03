using System.Collections.Generic;
using Netherlands3D.Functionalities.OBJImporter;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(OBJPropertyData))]
    public partial class MTLImportPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private ColorPropertyData stylingPropertyData;
        private OBJPropertyData objPropertyData;

        public MTLImportPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            objPropertyData = properties.Get<OBJPropertyData>();
            stylingPropertyData = properties.GetDefaultStylingPropertyData<ColorPropertyData>();
            objPropertyData.MtlImportSuccess.AddListener(OnMTLImportError);
        }
        
        //todo called in the inspector by FileOpen.cs
        public void ImportMtl(string path)
        {
            path = path.TrimEnd(',');

            if (!path.EndsWith(".mtl"))
                return;

            // When importing an MTL - we want to reset the coloring of the object
            stylingPropertyData.SetDefaultSymbolizerColor(null);

            SetMtlPathInPropertyData(path);

            SetNormalUIPanels();
        }


        private void SetMtlPathInPropertyData(string fullPath)
        {
            objPropertyData.MtlFile = AssetUriFactory.CreateProjectAssetUri(fullPath);
        }

        private void SetNormalUIPanels()
        {
            // importErrorPanel.SetActive(false);
            // if (!string.IsNullOrEmpty(objPropertyData.MtlFile?.ToString()))
            // {
            //     defaultImportPanel.SetActive(false);
            //     hasMtlPanel.SetActive(true);
            // }
        }

        private void OnMTLImportError(bool success)
        {
            if (success)
            {
                SetNormalUIPanels();
            }
            else
            {
                // importErrorPanel.SetActive(true);
                // defaultImportPanel.SetActive(false);
                // hasMtlPanel.SetActive(false);
            }
        }

    }
}