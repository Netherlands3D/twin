using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D.Twin
{
    public class QualitySettings : MonoBehaviour
    {
        [Header("Water Reflections")]
        [SerializeField] private GameObject waterReflectionsRenderer;
        private UniversalRenderPipelineAsset activeRenderPipelineAsset;
        [SerializeField] private ScriptableRendererFeature aoRenderFeature;

        void Awake()
        {
            activeRenderPipelineAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        }

        /// <summary>
        /// Set quality level for the Twin application
        /// </summary>
        /// <param name="qualityLevel">0 to 2 (Low, Medium, High respectively)</param>
        public void SetQualityLevel(int qualityLevel)
        {
            switch (qualityLevel)
            {
                case 0:
                    ToggleAA(false);
                    ToggleAO(false);
                    ToggleWaterReflections(false);
                    break;
                case 1:
                    ToggleAA(true);
                    ToggleAO(false);
                    ToggleWaterReflections(false);
                    break;
                case 2:
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
