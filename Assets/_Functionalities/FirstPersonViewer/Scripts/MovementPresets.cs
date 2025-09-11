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
        public float runningMultiplier;
        public float jumpHeight;
        public float maxFallingSpeed;
        public float stepHeight = 1;
        public ViewModus viewModus;

        [Header("Visuals")]
        public string viewName;
        public Sprite viewIcon;
        public Mesh viewMesh;
    }
    public enum ViewModus
    {
        STANDARD = 0,
        VEHICULAR = 1,
        FREECAM = 2
    }
}





