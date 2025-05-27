using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SetMaskCoordinates : MonoBehaviour
{
    // Match Shader Graph property names exactly
    [SerializeField] private string centerProperty = "_MaskBBoxCenter";
    [SerializeField] private string extentsProperty = "_MaskBBoxExtents";
    [SerializeField] private string invertProperty = "_MaskInvert";
    [SerializeField] private string textureProperty = "_MaskTexture";
    [SerializeField] private Texture2D testTexture;
    [SerializeField] private RenderTexture polygonTexture;
    [SerializeField] private bool useRenderTex;
    [SerializeField] private bool invertMask;

    [SerializeField] private DecalProjector projector;
    
    private void OnValidate()
    {
        SetBBoxVector();
    }

    private void LateUpdate()
    {
        transform.position = projector.transform.position;
        transform.localScale = new(projector.size.x, 1000, projector.size.y);
        if (transform.hasChanged)
        {
            SetBBoxVector();
            projector.GetComponent<Camera>().Render();
            transform.hasChanged = false;
        }
    }

    private void SetBBoxVector()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Renderer renderer = GetComponent<Renderer>();

        if (meshFilter.sharedMesh == null || renderer.sharedMaterial == null)
        {
            Debug.LogWarning("Mesh or material missing.");
            return;
        }

        Bounds bounds = meshFilter.sharedMesh.bounds;

        // Convert bounds from local space to world space
        Vector3 worldCenter = transform.position; //transform.TransformPoint(bounds.center);
        Vector3 worldExtents = Vector3.Scale(bounds.extents, transform.lossyScale);

        Vector2 worldCenterXZ = new Vector2(worldCenter.x, worldCenter.z);
        Vector2 worldExtentsXZ = new Vector2(worldExtents.x, worldExtents.z);

        Shader.SetGlobalVector(centerProperty, worldCenterXZ);
        Shader.SetGlobalVector(extentsProperty, worldExtentsXZ);
        Shader.SetGlobalInt(invertProperty, invertMask ? 1 : 0);
        if (useRenderTex)
            Shader.SetGlobalTexture(textureProperty, polygonTexture);
        else
            Shader.SetGlobalTexture(textureProperty, testTexture);
    }
}