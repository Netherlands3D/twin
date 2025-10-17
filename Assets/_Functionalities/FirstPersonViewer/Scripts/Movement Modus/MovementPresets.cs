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
        public float viewHeight;
        public float fieldOfView = 60;
        public float speedInKm;
        [Space(20)]
        public float speedMultiplier;
        public float maxFallingSpeed;
        public float stepHeight = 1;
        public string viewModus;

        [Header("Visuals")]
        public string viewName;
        public Sprite viewIcon;
        public Mesh viewMesh;
        public Material[] meshMaterials;

        [Header("Other")]
        public float groundResetHeightOffset;

        public ReorderableViewerSettingsList editableSettings = new ReorderableViewerSettingsList();
    }
}





