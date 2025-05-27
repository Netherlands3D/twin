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
    [SerializeField] private RenderTexture polygonTexture;
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
        Vector2 worldCenterXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 worldExtentsXZ = new Vector2(projector.size.x / 2, projector.size.y /2); //projector uses xy plane instead of xz plane

        Shader.SetGlobalVector(centerProperty, worldCenterXZ);
        Shader.SetGlobalVector(extentsProperty, worldExtentsXZ);
        Shader.SetGlobalInt(invertProperty, invertMask ? 1 : 0); 
        Shader.SetGlobalTexture(textureProperty, polygonTexture);
    }
}