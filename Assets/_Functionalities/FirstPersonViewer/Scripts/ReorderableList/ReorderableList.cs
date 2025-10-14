
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Temp
{
    [System.Serializable]
    public class ReorderableList<T>
    {
        [SerializeReference] 
        public List<T> list = new List<T>();
    }

    [System.Serializable]
    public class ReorderableViewerSettingsList : ReorderableList<ViewerSetting>
    {
    }
}
