using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class MTLImportPropertySection : MonoBehaviour
    {
        public ObjSpawner ObjSpawner { get; set; }

        //called in the inspector by FileOpen.cs
        public void ImportMTL(string path)
        {
            if (path.EndsWith(','))
                path = path.Remove(path.Length - 1);
            
            ObjSpawner.SetMtlPathInPropertyData(path);
            ObjSpawner.ReImport();
        }
    }
}