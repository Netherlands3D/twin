using System;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    [Serializable]
    public abstract class ViewerSetting
    {
        [HideInInspector] public string name => settingsLabel.name;


        [Header("Defaults")]
        public MovementLabel settingsLabel;
        public bool isVisible = true;

        public abstract object GetValue();
    }

    [Serializable]
    public class ViewerSettingValue : ViewerSetting
    {
        [Header("Settings")]
        public float defaultValue;

        [Space(10)]
        public float minValue;
        public float maxValue;

        public override object GetValue() => defaultValue;
    }

    [Serializable]
    public class ViewerSettingLabel : ViewerSetting
    {
        public override object GetValue() => 0f;
    }
}
