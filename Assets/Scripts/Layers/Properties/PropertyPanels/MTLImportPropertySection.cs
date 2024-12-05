using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class MTLImportPropertySection : MonoBehaviour
    {
        [SerializeField] private GameObject defaultImportPanel;
        [SerializeField] private GameObject hasMtlPanel;
        [SerializeField] private GameObject importErrorPanel;
        public ObjSpawner ObjSpawner { get; set; }
        
        private void Start()
        {
            SetUIPanels();
        }

        //called in the inspector by FileOpen.cs
        public void ImportMTL(string path)
        {
            if (path.EndsWith(','))
                path = path.Remove(path.Length - 1);
            
            ObjSpawner.SetMtlPathInPropertyData(path);
            ObjSpawner.StartImport();
            
            SetUIPanels();
        }
        
        private void SetUIPanels()
        {
            if (ObjSpawner.HasMtl)
            {
                defaultImportPanel.SetActive(false);
                hasMtlPanel.SetActive(true);
            }
        }
    }
}
