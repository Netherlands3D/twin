using System;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Temp
{
    [System.Serializable]
    public abstract class ViewerSetting
    {
        [HideInInspector] public string name => settingName;

        [Header("Defaults")]
        public string settingName;
        public string displayName;
        [Space()]
        public bool isVisible = true;
        public string units;

        public abstract object GetValue();

        public Action<object> OnValueChanged;
    }

    [System.Serializable]
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
        [Header("Settings")]
        public ViewerReplaceLabel[] replaceLabel;

        public override object GetValue() => replaceLabel;
    }

    [System.Serializable]
    public struct ViewerReplaceLabel
    {
        public string replaceLabel;
        public string replaceValue;
    }
}
