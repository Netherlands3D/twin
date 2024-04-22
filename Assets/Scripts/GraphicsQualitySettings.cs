using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public enum GraphicsQualityLevel
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public class GraphicsQualitySettings : MonoBehaviour
    {
        public static readonly UnityEvent<GraphicsQualityLevel> qualityLevelChanged = new();
        private static readonly string[] defaultToHighQualityVendorNames = new[] { "nvidia", "apple" };
        private static readonly string[] defaultToLowQualityVendorNames = new[] { "intel" };

        private const string QUALITY_SETTINGS_KEY = "QualitySettings";

        private void Awake()
        {
            InitializeQualitySettings();
        }

        private static void InitializeQualitySettings()
        {
            if (PlayerPrefs.HasKey(QUALITY_SETTINGS_KEY))
            {
                var savedQualitySettings = (GraphicsQualityLevel)PlayerPrefs.GetInt(QUALITY_SETTINGS_KEY);
                SetGraphicsQuality(savedQualitySettings, false);
                return; // if the user set something specifically, use this instead of defaults0
            }

            var initialQualitySettings = GraphicsQualityLevel.Medium;

            var graphicsDeviceVendor = SystemInfo.graphicsDeviceName;
            foreach (var vendor in defaultToHighQualityVendorNames)
            {
                if (graphicsDeviceVendor.ToLower().Contains(vendor))
                {
                    initialQualitySettings = GraphicsQualityLevel.High;
                    break;
                }
            }

            foreach (var vendor in defaultToLowQualityVendorNames)
            {
                if (graphicsDeviceVendor.ToLower().Contains(vendor))
                {
                    initialQualitySettings = GraphicsQualityLevel.Low;
                    break;
                }
            }

            SetGraphicsQuality(initialQualitySettings, false);
        }

        public static void SetGraphicsQuality(GraphicsQualityLevel level, bool saveSetting)
        {
            Debug.Log("setting ql to : " + level);
            UnityEngine.QualitySettings.SetQualityLevel((int)level);

            if (saveSetting)
                PlayerPrefs.SetInt(QUALITY_SETTINGS_KEY, (int)level);
            
            qualityLevelChanged.Invoke(level);
        }
    }
}