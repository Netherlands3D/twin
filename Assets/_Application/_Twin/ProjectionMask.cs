using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D
{
    public class ProjectionMask : MonoBehaviour
    {
        [SerializeField] private string centerProperty = "_MaskBBoxCenter";
        [SerializeField] private string extentsProperty = "_MaskBboxExtents";
        [SerializeField] private string invertProperty = "_MaskInvert";
        [SerializeField] private string renderTextureProperty = "_MaskTexture";
        [SerializeField] private bool invertMask;
        [SerializeField] private RenderTexture tex;
        
        private DecalProjector decalProjector;

        private void Awake()
        {
            decalProjector = GetComponent<DecalProjector>();
        }

        private void OnValidate()
        {
            // SetBBoxVectors();
        }

        private void Update()
        {
            // if (transform.hasChanged)
            // {
                SetBBoxVectors();
            //     transform.hasChanged = false;
            // }
        }

        private void SetBBoxVectors()
        {
            Bounds worldBounds = new Bounds(transform.position, decalProjector.size);

            Vector2 worldCenterXZ = new Vector2(worldBounds.center.x, worldBounds.center.z);
            Vector2 worldExtentsXZ = new Vector2(worldBounds.extents.x, worldBounds.extents.z);

            Shader.SetGlobalTexture(renderTextureProperty, tex);
            Shader.SetGlobalVector(centerProperty, worldCenterXZ);
            Shader.SetGlobalVector(extentsProperty, worldExtentsXZ);
            Shader.SetGlobalInt(invertProperty, invertMask ? 1 : 0);

var a =            Shader.GetGlobalInt(invertProperty);
        print(a);
        }
    }
}
