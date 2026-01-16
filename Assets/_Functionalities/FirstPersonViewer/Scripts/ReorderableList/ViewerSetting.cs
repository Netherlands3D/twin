using System;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    [Serializable]
    public abstract class ViewerSetting
    {
        [Header("Defaults")]
        public bool isVisible = true;

        public abstract object GetValue();
        public abstract object GetDefaultValue();
        public abstract string GetDisplayName();
        public abstract string GetDisplayUnits();

        public abstract void InvokeOnValueChanged(object value);
    }

    // We unfortunately need this class since a generic class can not be drawn with the PropertyDrawer
    public abstract class ViewerSettingGeneric<T> : ViewerSetting 
    {
        public MovementSetting<T> movementSetting;
        public override string GetDisplayName() => movementSetting.displayName;
        public override string GetDisplayUnits() => movementSetting.units;
        public override object GetValue() => movementSetting.Value;

        public override void InvokeOnValueChanged(object value)
        {
            if (value is T typeValue)
            {
                movementSetting.Value = typeValue;
            }
            else
            {
                throw new InvalidCastException("value is not of type " + typeof(T));
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

        public override object GetDefaultValue() => defaultValue;
    }

    [Serializable]
    public class ViewerSettingLabel : ViewerSettingGeneric<string>
    {
        public override object GetDefaultValue() => "";
    }

    [Serializable]
    public class ViewerSettingBool : ViewerSettingGeneric<bool>
    {
        [Header("Settings")]
        public bool defaultValue;

        public override object GetDefaultValue() => defaultValue;
    }
}
