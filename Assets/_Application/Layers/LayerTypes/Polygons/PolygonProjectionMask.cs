using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D
{
    public class PolygonProjectionMask : MonoBehaviour
    {
        // Match Shader Graph property names exactly
        [Header("Mask settings")]
        [SerializeField] private string centerProperty = "_MaskBBoxCenter";
        [SerializeField] private string extentsProperty = "_MaskBBoxExtents";
        [SerializeField] private string invertProperty = "_MaskInvert";
        [SerializeField] private string textureProperty = "_MaskTexture";
        [SerializeField] private bool invertMask;
        
        private DecalProjector decalProjector;
        private Camera projectionCamera;
        
        private void Awake()
        {
            decalProjector = GetComponent<DecalProjector>();
            projectionCamera = GetComponent<Camera>();
        }

        private void Start()
        {
            Shader.SetGlobalTexture(textureProperty, projectionCamera.targetTexture);
        }

        // Update is called once per frame
        void LateUpdate() //use LateUpdate to ensure the transform changes have applied before setting the Shader vectors
        {
            if (transform.hasChanged)
            {
                SetShaderMaskVectors();
                transform.hasChanged = false;
            }
        }
        
        private void SetShaderMaskVectors()
        {
            Vector2 worldCenterXZ = new Vector2(transform.position.x, transform.position.z);
            Vector2 worldExtentsXZ = new Vector2(decalProjector.size.x / 2, decalProjector.size.y /2); //projector uses xy plane instead of xz plane

            Shader.SetGlobalVector(centerProperty, worldCenterXZ);
            Shader.SetGlobalVector(extentsProperty, worldExtentsXZ);
            Shader.SetGlobalInt(invertProperty, invertMask ? 1 : 0); 
        }
    }
}
