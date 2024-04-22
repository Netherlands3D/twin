using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D.Twin
{
    public class QualitySettings : MonoBehaviour
    {
        [Header("Water Reflections")]
        [SerializeField] private GameObject waterReflectionsRenderer;

        void Awake()
        {
            var urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
           
        }

        public void SetPlanarWaterReflections(bool enable)
        {
            waterReflectionsRenderer.SetActive(enable);
        }
    }
}
