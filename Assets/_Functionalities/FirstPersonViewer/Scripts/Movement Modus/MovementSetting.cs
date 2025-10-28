using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    public abstract class MovementSetting<T> : ScriptableObject
    {
        public string settingName;

        public string displayName;
        public string units;

        public UnityEvent<T> OnValueChanged = new();
    }
}
