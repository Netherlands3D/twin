using Netherlands3D.FirstPersonViewer.ViewModus;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D
{
    [CreateAssetMenu(fileName = "MovementCollection", menuName = "ScriptableObjects/FirstPersonViewer/Movement Collection")]
    public class MovementCollection : ScriptableObject
    {
        public List<MovementPresets> presets;
    }
}
