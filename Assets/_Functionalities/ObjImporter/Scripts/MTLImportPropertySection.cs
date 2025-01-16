using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D.Functionalities.OBJImporter
{
    public class MTLImportPropertySection : MonoBehaviour
    {
        [SerializeField] private GameObject defaultImportPanel;
        [SerializeField] private GameObject hasMtlPanel;
        [SerializeField] private GameObject importErrorPanel;
        public OBJSpawner ObjSpawner { get; set; }
        
        private void Start()
        {
            SetNormalUIPanels();
            ObjSpawner.MtlImportSuccess.AddListener(OnMTLImportError);
        }

        //called in the inspector by FileOpen.cs
        public void ImportMTL(string path)
        {
            if (path.EndsWith(','))
                path = path.Remove(path.Length - 1);
            
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
