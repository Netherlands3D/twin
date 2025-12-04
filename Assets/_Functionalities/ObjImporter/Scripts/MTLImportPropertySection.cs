using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Functionalities.OBJImporter
{
    [PropertySection(typeof(OBJPropertyData))]
    public class MTLImportPropertySection :  MonoBehaviour, IVisualizationWithPropertyData
    {
        [SerializeField] private GameObject defaultImportPanel;
        [SerializeField] private GameObject hasMtlPanel;
        [SerializeField] private GameObject importErrorPanel;

        private StylingPropertyData stylingPropertyData;
        private OBJPropertyData objPropertyData;

        private void Start()
        {
            //layer = ObjSpawner.GetComponent<HierarchicalObjectLayerGameObject>();
            SetNormalUIPanels();
            //ObjSpawner.MtlImportSuccess.AddListener(OnMTLImportError);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            objPropertyData = properties.Get<OBJPropertyData>();
            objPropertyData.MtlImportSuccess.AddListener(OnMTLImportError);

            stylingPropertyData = properties.Get<StylingPropertyData>();
        }

        //called in the inspector by FileOpen.cs
        public void ImportMtl(string path)
        {
            path = path.TrimEnd(',');

            // When importing an MTL - we want to reset the coloring of the object
            //HierarchicalObjectLayerStyler.ResetColoring(layer);

            stylingPropertyData.SetDefaultSymbolizerFillColor(null);

            SetMtlPathInPropertyData(path);
            
            SetNormalUIPanels();
        }


        private void SetMtlPathInPropertyData(string fullPath)
        {          
            objPropertyData.MtlFile = AssetUriFactory.CreateProjectAssetUri(fullPath);
        }

        private void SetNormalUIPanels()
        {
            importErrorPanel.SetActive(false);
            if (!string.IsNullOrEmpty(objPropertyData.MtlFile.ToString()))
            {
                defaultImportPanel.SetActive(false);
                hasMtlPanel.SetActive(true);
            }
        }

        private void OnMTLImportError(bool success)
        {
            if (success)
            {
                SetNormalUIPanels();
            }
            else
            {
                importErrorPanel.SetActive(true);
                defaultImportPanel.SetActive(false);
                hasMtlPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            objPropertyData.MtlImportSuccess.RemoveListener(OnMTLImportError);
        }
    }
}
