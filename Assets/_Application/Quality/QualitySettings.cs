using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D.Twin.Quality
{
    public enum GraphicsQualityLevel
    {
        Low = 0,
        Medium = 1,
        High = 2
    }


    public class QualitySettings : MonoBehaviour
    {
        [Header("Water Reflections")] [SerializeField]
        private GameObject waterReflectionsRenderer;

        private UniversalRenderPipelineAsset activeRenderPipelineAsset;
        [SerializeField] private ScriptableRendererFeature aoRenderFeature;

        public static readonly UnityEvent<GraphicsQualityLevel> qualityLevelChanged = new();
        private static readonly string[] defaultToHighQualityVendorNames = new[] { "nvidia", "apple" };
        private static readonly string[] defaultToLowQualityVendorNames = new[] { "intel" };

        private const string QUALITY_SETTINGS_KEY = "QualitySettings";

        private void Start()
        {
            activeRenderPipelineAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            InitializeQualitySettings();
        }

        private void OnEnable()
        {
            qualityLevelChanged.AddListener(SetVisualEffectsForQualityLevel); //via event because the SetGraphicsQuality method is static
        }

        private void OnDisable()
        {
            qualityLevelChanged.RemoveListener(SetVisualEffectsForQualityLevel);
        }

        private void InitializeQualitySettings()
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

        /// <summary>
        /// Set quality level for the Twin application
        /// </summary>
        /// <param name="qualityLevel">0 to 2 (Low, Medium, High respectively)</param>
        public static void SetGraphicsQuality(GraphicsQualityLevel level, bool saveSetting)
        {
            Debug.Log("setting quality level to : " + level);
            var levelIndex = (int)level;
            
            UnityEngine.QualitySettings.SetQualityLevel(levelIndex);

            if (saveSetting)
                PlayerPrefs.SetInt(QUALITY_SETTINGS_KEY, levelIndex);
            
            qualityLevelChanged.Invoke(level);
        }
        
        private void SetVisualEffectsForQualityLevel(GraphicsQualityLevel level)
        {
            switch (level)
            {
                case GraphicsQualityLevel.Low:
                    ToggleAA(false);
                    ToggleAO(false);
                    ToggleWaterReflections(false);
                    break;
                case GraphicsQualityLevel.Medium:
                    ToggleAA(true);
                    ToggleAO(false);
                    ToggleWaterReflections(false);
                    break;
                case GraphicsQualityLevel.High:
                    ToggleAA(true);
                    ToggleAO(true);
                    ToggleWaterReflections(true);
                    break;
            }
        }

        /// <summary>
        /// Toggle Screen Space Ambient Occlusion render feature
        /// </summary>
        /// <param name="enabled">Render feature active state</param>
        public void ToggleAO(bool enabled)
        {
            aoRenderFeature.SetActive(enabled);
        }

        /// <summary>
        /// Toggle Fast Approximate Antialiasing on main camera
        /// </summary>
        /// <param name="enabled">Enabled FXAA</param>
        public void ToggleAA(bool enabled)
        {
            UniversalAdditionalCameraData universalCameraData = Camera.main.GetComponent<UniversalAdditionalCameraData>();
            universalCameraData.antialiasing = enabled ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.None;
        }

        /// <summary>
        /// Enable or disable realtime planar water reflections
        /// </summary>
        /// <param name="enabled">Enabled water reflections</param>
        public void ToggleWaterReflections(bool enabled)
        {
            waterReflectionsRenderer.SetActive(enabled);
        }
    }
}