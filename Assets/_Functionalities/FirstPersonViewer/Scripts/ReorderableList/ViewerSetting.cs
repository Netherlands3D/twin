using System;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    [Serializable]
    public abstract class ViewerSetting
    {
        [Header("Defaults")]
        public bool isVisible = true;

        //We use floats for now.
        public abstract float GetValue();
        public abstract string GetDisplayName();
        public abstract string GetDisplayUnits();

        public abstract void InvokeOnValueChanged(object value);
    }

    public abstract class ViewerSettingGeneric<T> : ViewerSetting
    {
        public MovementSetting<T> movementSetting;
        public override string GetDisplayName() => movementSetting.displayName;
        public override string GetDisplayUnits() => movementSetting.units;

        public override void InvokeOnValueChanged(object value)
        {
            if (value is T typeValue)
            {
                movementSetting.OnValueChanged.Invoke(typeValue);
            }
        }
    }

    [Serializable]
    public class ViewerSettingValue : ViewerSettingGeneric<float>
    {
        [Header("Settings")]
        public float defaultValue;

        [Space(10)]
        public float minValue;
        public float maxValue;

        public override float GetValue() => defaultValue;
    }

    [Serializable]
    public class ViewerSettingLabel : ViewerSettingGeneric<string>
    {
        public override float GetValue() => 0f;
    }
}
