using System;
using UnityEngine;
namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    [CreateAssetMenu(fileName = "Movement Presets", menuName = "ScriptableObjects/FirstPersonViewer/Movement Presets")]
    public class MovementPresets : ScriptableObject
    {
        [Header("Movement")]
        public float viewHeight;
        public float fieldOfView = 60;
        public float speedInKm;
        [Space(20)]
        public float speedMultiplier;
        public float jumpHeight;
        public float maxFallingSpeed;
        public float stepHeight = 1;
        public ViewModus viewModus;

        [Header("Vehicle Settings")]
        public float acceleration;
        public float deceleration;
        public float turnSpeed;

        [Header("Visuals")]
        public string viewName;
        public Sprite viewIcon;
        public Mesh viewMesh;
        public Material[] meshMaterials;

        public Type GetViewerState() //Not user friendly for edits. (Prob some sort of subscribe system?)
        {
            switch (viewModus)
            {
                default:
                case ViewModus.STANDARD:
                    return typeof(ViewerWalkingState);
                case ViewModus.VEHICULAR:
                    return typeof(ViewerVehicularState);
                case ViewModus.FREECAM:
                    return typeof(ViewerFlyingState);
            }
        }
        public enum ViewModus //Not user friendly for edits. (Prob some sort of subscribe system?)
        {
            STANDARD = 0,
            VEHICULAR = 1,
            FREECAM = 2
        }
    }
}





