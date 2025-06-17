using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using UnityEngine;

namespace Netherlands3D.Functionalities.OBJImporter
{
    public class MTLImportPropertySection : MonoBehaviour
    {
        [SerializeField] private GameObject defaultImportPanel;
        [SerializeField] private GameObject hasMtlPanel;
        [SerializeField] private GameObject importErrorPanel;
        private HierarchicalObjectLayerGameObject layer;
        public OBJSpawner ObjSpawner { get; set; }
        
        private void Start()
        {
            layer = ObjSpawner.GetComponent<HierarchicalObjectLayerGameObject>();
            SetNormalUIPanels();
            ObjSpawner.MtlImportSuccess.AddListener(OnMTLImportError);
        }

        //called in the inspector by FileOpen.cs
        public void ImportMtl(string path)
        {
            path = path.TrimEnd(',');

            // When importing an MTL - we want to reset the coloring of the object
            HierarchicalObjectTileLayerStyler.ResetColoring(layer);

            ObjSpawner.SetMtlPathInPropertyData(path);
            ObjSpawner.StartImport();
            
            SetNormalUIPanels();
        }
        
        private void SetNormalUIPanels()
        {
            importErrorPanel.SetActive(false);
            if (ObjSpawner.HasMtl)
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
            ObjSpawner.MtlImportSuccess.RemoveListener(OnMTLImportError);
        }
    }
}
