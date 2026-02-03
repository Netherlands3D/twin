using System;
using System.Collections;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using UnityEngine;

namespace Netherlands3D
{
    public class PolygonProjectionMask : MonoBehaviour
    {
        // Match Shader Graph property names exactly
        [Header("Mask settings")] 
        [SerializeField] private string centerProperty = "_MaskBBoxCenter";

        [SerializeField] private string extentsProperty = "_MaskBBoxExtents";
        [SerializeField] private string maskTextureProperty = "_MaskTexture";
        private const string usedInvertedMaskChannelsProperty = "_UsedInvertedMaskChannels";
        
        [SerializeField] private Camera maskCamera;

        private static bool forceUpdate;
        private static int usedInvertedMasks = 0;

        private void OnEnable()
        {
            PolygonSelectionLayerPropertyData.MaskAdded.AddListener(ForceUpdateVectorsAtEndOfFrame);
            PolygonSelectionLayerPropertyData.MaskRemoved.AddListener(ForceUpdateVectorsAtEndOfFrame);
        }

        private void OnDisable()
        {
            PolygonSelectionLayerPropertyData.MaskAdded.RemoveListener(ForceUpdateVectorsAtEndOfFrame);
            PolygonSelectionLayerPropertyData.MaskRemoved.RemoveListener(ForceUpdateVectorsAtEndOfFrame);
        }

        private IEnumerator Start()
        {
            Shader.SetGlobalTexture(maskTextureProperty, maskCamera.targetTexture);
            Shader.SetGlobalInt(usedInvertedMaskChannelsProperty, usedInvertedMasks); //initialize the correct value

            yield return null;
            ForceUpdateVectorsAtEndOfFrame();
        }

        private void LateUpdate() //use LateUpdate to ensure the transform changes have applied before setting the Shader vectors
        {
            if (forceUpdate || transform.hasChanged)
            {
                SetShaderMaskVectors();
                // when there are 0 inverted masks, all geometry should be visible, so we should change the shader to output alpha=1 on all pixels without an inverted mask, otherwise we need to set alpha=0 so the environment is masked away
                maskCamera.Render(); //force a render so the texture is ready to be sampled by the regular pipeline
                transform.hasChanged = false;
                forceUpdate = false;
            }
        }

        private void SetShaderMaskVectors()
        {
            Vector2 worldCenterXZ = new Vector2(maskCamera.transform.position.x, maskCamera.transform.position.z);
            Vector2 worldExtentsXZ = new Vector2(maskCamera.orthographicSize, maskCamera.orthographicSize); //projector uses xy plane instead of xz plane

            Shader.SetGlobalVector(centerProperty, worldCenterXZ);
            Shader.SetGlobalVector(extentsProperty, worldExtentsXZ);
        }

        private void ForceUpdateVectorsAtEndOfFrame(int maskBit) // call this when updating the polygons that should be used as masks to update the texture at the end of this frame
        {
            ForceUpdateVectorsAtEndOfFrame();
        }
        
        public static void ForceUpdateVectorsAtEndOfFrame() // call this when updating the polygons that should be used as masks to update the texture at the end of this frame
        {
            forceUpdate = true;
        }

        private static void AddInvertedMask(int maskBitIndex)
        {
            usedInvertedMasks |= 1 << maskBitIndex;
            Shader.SetGlobalInt(usedInvertedMaskChannelsProperty, usedInvertedMasks);
        }

        private static void RemoveInvertedMask(int maskBitIndex)
        {
            usedInvertedMasks &= ~(1 << maskBitIndex);
            Shader.SetGlobalInt(usedInvertedMaskChannelsProperty, usedInvertedMasks);
        }

        public static void UpdateInvertedMaskBit(int maskBitIndex, bool setActive)
        {
            if(setActive)
                AddInvertedMask(maskBitIndex);
            else
                RemoveInvertedMask(maskBitIndex);
        }
    }
}