using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] private string maskInvertTextureProperty = "_MaskInvertTexture";
        [SerializeField] private string maskTextureProperty = "_MaskTexture";
        
        [SerializeField] private Camera maskCamera;
        [SerializeField] private Camera maskInvertCamera;

        private static bool forceUpdate;
        private static readonly HashSet<GameObject> invertedMasks = new(); // when there are 0 inverted masks, all geometry should be visible, so we should change the output texture to alpha=1 on all pixels.

        private IEnumerator Start()
        {
            Shader.SetGlobalTexture(maskTextureProperty, maskCamera.targetTexture);
            Shader.SetGlobalTexture(maskInvertTextureProperty, maskInvertCamera.targetTexture);

            yield return null;
            ForceUpdateVectorsAtEndOfFrame();
        }

        private void LateUpdate() //use LateUpdate to ensure the transform changes have applied before setting the Shader vectors
        {
            if (forceUpdate || transform.hasChanged)
            {
                SetShaderMaskVectors();
                UpdateCameraBackgroundColor();
                maskCamera.Render(); //force a render so the texture is ready to be sampled by the regular pipeline
                maskInvertCamera.Render(); //force a render so the texture is ready to be sampled by the regular pipeline
                transform.hasChanged = false;
                forceUpdate = false;
            }
        }
        
        // when there are 0 inverted masks, all geometry should be visible, so we should change the output texture to alpha=1 on all pixels, otherwise we need to set alpha=0 so the environment is masked away
        private void UpdateCameraBackgroundColor()
        {
            if(invertedMasks.Count > 0)
                maskInvertCamera.backgroundColor = Color.clear;
            else
                maskInvertCamera.backgroundColor = Color.white;
        }
        
        private void SetShaderMaskVectors()
        {
            Vector2 worldCenterXZ = new Vector2(maskCamera.transform.position.x, maskCamera.transform.position.z);
            Vector2 worldExtentsXZ = new Vector2(maskCamera.orthographicSize, maskCamera.orthographicSize); //projector uses xy plane instead of xz plane

            Shader.SetGlobalVector(centerProperty, worldCenterXZ);
            Shader.SetGlobalVector(extentsProperty, worldExtentsXZ);
        }

        public static void ForceUpdateVectorsAtEndOfFrame() // call this when updating the polygons that should be used as masks to update the texture at the end of this frame
        {
            forceUpdate = true;
        }

        public static void AddInvertedMask(GameObject invertedMask)
        { 
            invertedMasks.Add(invertedMask);
        }

        public static void RemoveInvertedMask(GameObject invertedMask)
        {
            invertedMasks.Remove(invertedMask);
        }
    }
}
