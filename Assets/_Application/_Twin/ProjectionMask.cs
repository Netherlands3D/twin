using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D
{
    public class ProjectionMask : MonoBehaviour
    {
        [SerializeField] private string centerProperty = "_MaskBBoxCenter";
        [SerializeField] private string extentsProperty = "_MaskBBoxExtents";
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

        public Material mat;
        public Texture2D testTex;
        private void SetBBoxVectors()
        {
            Vector2 worldCenterXZ = new Vector2(transform.position.x, transform.position.z);
            Vector2 worldExtentsXZ = new Vector2(decalProjector.size.x /2, decalProjector.size.y/2);
            
            Shader.SetGlobalTexture(renderTextureProperty, testTex);
            Shader.SetGlobalVector(centerProperty, worldCenterXZ);
            Shader.SetGlobalVector(extentsProperty, worldExtentsXZ);
            Shader.SetGlobalInt(invertProperty, invertMask ? 1 : 0);
        }

        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var c = Shader.GetGlobalVector(centerProperty);
            var s = 2 * Shader.GetGlobalVector(extentsProperty);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(new(c.x, 0, c.y), new(s.x, 100, s.y));
        }
        #endif
    }
}
