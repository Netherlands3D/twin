using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    [CreateAssetMenu(fileName = "Movement Presets", menuName = "ScriptableObjects/FirstPersonViewer/Movement Presets")]
    public class MovementPresets : ScriptableObject
    {
        public ViewerState viewerState;

        [Header("Visuals")]
        public string viewName;
        public Sprite viewIcon;
        public Mesh viewMesh;
        public Material[] meshMaterials;

        [Header("Editable Settings")]
        public ReorderableViewerSettingsList editableSettings = new ReorderableViewerSettingsList();
    }
}





