using Netherlands3D.FirstPersonViewer.Events;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer
{
    public class FirstPersonViewerData : MonoBehaviour
    {
        //Maybe move the settings data to the ScriptableObject instead of having a string based 
        private Dictionary<string, object> viewerSetting;

        [field:SerializeField] public Camera FPVCamera { private set; get; }
        [field:SerializeField] public MovementModusSwitcher ModusSwitcher { private set; get; } 

        private void OnEnable()
        {
            FPVCamera = GetComponentInChildren<Camera>();
            viewerSetting = new Dictionary<string, object>();

            ViewerEvents.onSettingChanged += SettingsChanged;
        }

        private void OnDestroy()
        {
            ViewerEvents.onSettingChanged -= SettingsChanged;
        }

        private void SettingsChanged(string setting, object value)
        {
            if(viewerSetting.ContainsKey(setting)) viewerSetting[setting] = value;
            else viewerSetting.Add(setting, value);
        }

        public object GetSettingValue(string setting)
        {
            if (viewerSetting.ContainsKey(setting)) return viewerSetting[setting];
            else return null;
        }

        public bool TryGetValue(string setting, out object value)
        {
            return viewerSetting.TryGetValue(setting, out value);
        }
    }
}
