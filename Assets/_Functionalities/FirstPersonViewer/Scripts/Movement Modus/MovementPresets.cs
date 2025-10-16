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
        public string viewModus;

        [Header("Vehicle Settings")]
        public float acceleration;
        public float deceleration;
        public float turnSpeed;

        [Header("Visuals")]
        public string viewName;
        public Sprite viewIcon;
        public Mesh viewMesh;
        public Material[] meshMaterials;

        [Header("Other")]
        public float groundResetHeightOffset;
    }
}





