using UnityEngine;

public static class NL3DShaders
{
    public static Shader UnlitShader;
    public static Shader MainLayersShader;
    public static Shader WaterShader; 
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
