using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class PointCloudAutoSize : MonoBehaviour
{
    public Camera targetCamera;
    public float baseSize = 300f;
    public float minSize = 2f;
    public float maxSize = 18f;

    private Renderer _renderer;
    private Material _runtimeMat;   // instanced
    private static readonly int PointSizeId = Shader.PropertyToID("_PointSize");

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        // force an instance so we are NOT editing sharedMaterial
        _runtimeMat = _renderer.material;

        if (!targetCamera)
            targetCamera = Camera.main;

        Debug.Log($"[PointCloudAutoSizeDebug] Awake on {name}. Material = {_runtimeMat.shader.name}");
    }

    void LateUpdate()
    {
        if (!_runtimeMat || !targetCamera) return;

        float dist = Vector3.Distance(targetCamera.transform.position, transform.position);
        if (dist < 0.01f) dist = 0.01f;

        float size = baseSize / dist;
        size = Mathf.Clamp(size, minSize, maxSize);

        _runtimeMat.SetFloat(PointSizeId, size);

        // debug every ~0.5s
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"[PointCloudAutoSizeDebug] dist={dist:F2} -> size={size:F2}");
        }
    }
}
