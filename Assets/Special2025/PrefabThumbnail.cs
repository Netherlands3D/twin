using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrefabThumbnail : MonoBehaviour
{
    [SerializeField] private int textureSize = 128;
    
    private Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    public Sprite GetThumbnail(GameObject prefab)
    {
        if(sprites.ContainsKey(prefab.name))
            return sprites[prefab.name];
        
        // Create a temporary instance
        GameObject instance = Instantiate(prefab);
        
        Quaternion isoRotation = Quaternion.Euler(30f, 45f, 0f);
        instance.transform.rotation = isoRotation;
        
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                mat.shader = unlitShader;
            }
        }

        // Calculate bounds for positioning
        Bounds bounds = CalculateBounds(instance);

        // Create a temporary camera
        GameObject camGO = new GameObject("ThumbnailCam");
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.clear;
        cam.orthographic = false; // perspective
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 100f;
        cam.cullingMask = LayerMask.GetMask("Default"); // adjust if needed
        
        GameObject lightGO = new GameObject("ThumbnailLight");
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f; // adjust as needed
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

        float distance = bounds.size.magnitude; // adjust multiplier as needed
        Vector3 isoOffset = new Vector3(1, 1, -1).normalized * distance; // isometric offset
        cam.transform.position = bounds.center + isoOffset;
        cam.transform.LookAt(bounds.center);
        cam.SetReplacementShader(Shader.Find("Universal Render Pipeline/Unlit"), null);

// Setup RenderTexture and render
        RenderTexture rt = new RenderTexture(textureSize, textureSize, 16);
        cam.targetTexture = rt;
        cam.Render();

        // Convert to Texture2D
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
        tex.Apply();

        // Assign to UI
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);

        // Cleanup
        cam.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
        Destroy(camGO);
        Destroy(instance);
        Destroy(lightGO);
        
        sprites.Add(prefab.name, sprite);
        
        return sprite; 
    }

    private Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.one);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);
        return bounds;
    }
}