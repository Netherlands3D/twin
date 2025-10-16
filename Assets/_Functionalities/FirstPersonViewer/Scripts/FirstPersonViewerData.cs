using Netherlands3D.FirstPersonViewer.Events;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer
{
    public class FirstPersonViewerData : MonoBehaviour
    {
        public Dictionary<string, object> ViewerSetting { get; private set; }

        [field:SerializeField] public Camera FPVCamera { private set; get; }
        [field:SerializeField] public MovementModusSwitcher ModusSwitcher { private set; get; } 

        private void OnEnable()
        {
            FPVCamera = GetComponentInChildren<Camera>();
            ViewerSetting = new Dictionary<string, object>();

            ViewerEvents.onSettingChanged += SettingsChanged;
        }

        private void OnDestroy()
        {
            ViewerEvents.onSettingChanged -= SettingsChanged;
        }

        private void SettingsChanged(string setting, object value)
        {
            if(ViewerSetting.ContainsKey(setting)) ViewerSetting[setting] = value;
            else ViewerSetting.Add(setting, value);
        }

        public object GetSettingValue(string setting)
        {
            if (ViewerSetting.ContainsKey(setting)) return ViewerSetting[setting];
            else return null;
        }
    }
}
