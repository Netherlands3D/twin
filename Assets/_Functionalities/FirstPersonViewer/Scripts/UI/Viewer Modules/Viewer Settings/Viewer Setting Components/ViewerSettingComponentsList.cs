using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    [CreateAssetMenu(fileName = "ViewerSettingComponentsList", menuName = "ScriptableObjects/FirstPersonViewer/Viewer Setting Components List")]
    public class ViewerSettingComponentsList : ScriptableObject
    {
        public List<ViewerSettingPrefab> settingPrefabs;
    }

    [System.Serializable]
    public class ViewerSettingPrefab
    {
        public string className;
        public ViewerSettingComponent prefab;
    }
}
