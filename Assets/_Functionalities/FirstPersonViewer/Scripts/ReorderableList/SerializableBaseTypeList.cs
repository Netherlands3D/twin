
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    [System.Serializable]
    public class SerializableBaseTypeList<T>
    {
        [SerializeReference] 
        public List<T> list = new List<T>();
    }

    [System.Serializable]
    public class SerializableViewerSettingsList : SerializableBaseTypeList<ViewerSetting>
    {
    }
}
