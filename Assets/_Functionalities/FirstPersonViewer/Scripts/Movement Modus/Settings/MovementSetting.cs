using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    public abstract class MovementSetting<T> : ScriptableObject
    {
        public string settingName;

        public string displayName;
        public string units;

        private T value;
        public T Value
        {
            set
            {
                this.value = value;
                OnValueChanged.Invoke(value);
            }
            get => value;
        }

        public UnityEvent<T> OnValueChanged = new();
    }
}
