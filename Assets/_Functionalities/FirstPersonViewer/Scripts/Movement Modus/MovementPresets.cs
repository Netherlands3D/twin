using Netherlands3D.FirstPersonViewer.Temp;
using System;
using UnityEngine;
namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    [CreateAssetMenu(fileName = "Movement Presets", menuName = "ScriptableObjects/FirstPersonViewer/Movement Presets")]
    public class MovementPresets : ScriptableObject
    {
        public ViewerState viewerState;

        [Header("Movement")]
        public float maxFallingSpeed;
        public float stepHeight = 1;

        [Header("Visuals")]
        public string viewName;
        public Sprite viewIcon;
        public Mesh viewMesh;
        public Material[] meshMaterials;

        [Header("Editable Settings")]
        public ReorderableViewerSettingsList editableSettings = new ReorderableViewerSettingsList();
    }
}





