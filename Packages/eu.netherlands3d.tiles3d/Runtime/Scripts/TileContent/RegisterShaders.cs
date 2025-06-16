using UnityEngine;

namespace Netherlands3D.Tiles3D
{
    public static class NL3DShaders
    {
        public static Shader UnlitShader;
        public static Shader MainLayersShader;
        public static Shader WaterShader;
        
        /// <summary>Shader property ID for property MainTexture</summary>
        public static int MainTextureShaderProperty = Shader.PropertyToID("_MainTexture");

        /// <summary>Shader property ID for property BaseColor</summary>
        public static int BaseColorShaderProperty = Shader.PropertyToID("_BaseColor");

        /// <summary>Shader property ID for property Metallic</summary>
        public static int MetallicShaderProperty = Shader.PropertyToID("_Metallic");

        /// <summary>Shader property ID for property Smoothness</summary>
        public static int SmoothnessShaderProperty = Shader.PropertyToID("_Smoothness");
    }

    public class RegisterShaders : MonoBehaviour
    {
        [SerializeField] private Shader unlitShader;
        [SerializeField] private Shader mainLayersShader;
        [SerializeField] private Shader waterShader;

        private void Awake()
        {
            NL3DShaders.UnlitShader = unlitShader;
            NL3DShaders.MainLayersShader = mainLayersShader;
            NL3DShaders.WaterShader = waterShader;
        }
    }
}