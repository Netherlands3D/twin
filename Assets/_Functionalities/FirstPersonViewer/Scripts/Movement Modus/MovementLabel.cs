using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    [CreateAssetMenu(fileName = "Movement Label", menuName = "ScriptableObjects/FirstPersonViewer/Movement Label")]
    public class MovementLabel : ScriptableObject
    {
        public string settingName;

        public string displayName;
        public string units;
    }
}
