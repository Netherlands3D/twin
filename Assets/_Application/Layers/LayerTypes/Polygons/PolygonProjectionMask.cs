using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] private string enableInvertMasksProperty = "_EnableInvertMasks";
        [SerializeField] private string usedMaskChannelsProperty = "_UsedMaskChannels";
        
        [SerializeField] private Camera maskCamera;

        private static bool forceUpdate;
        private static readonly HashSet<GameObject> invertedMasks = new(); // when there are 0 inverted masks, all geometry should be visible, so we should change the output texture to alpha=1 on all pixels.

        private IEnumerator Start()
        {
            Shader.SetGlobalTexture(maskTextureProperty, maskCamera.targetTexture);

            yield return null;
            ForceUpdateVectorsAtEndOfFrame();
        }

        private void LateUpdate() //use LateUpdate to ensure the transform changes have applied before setting the Shader vectors
        {
            if (forceUpdate || transform.hasChanged)
            {
                SetShaderMaskVectors();
                // when there are 0 inverted masks, all geometry should be visible, so we should change the shader to output alpha=1 on all pixels without an inverted mask, otherwise we need to set alpha=0 so the environment is masked away
                Shader.SetGlobalInteger(enableInvertMasksProperty,  invertedMasks.Count > 0 ? 1 : 0);
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

        public static void ForceUpdateVectorsAtEndOfFrame() // call this when updating the polygons that should be used as masks to update the texture at the end of this frame
        {
            forceUpdate = true;
        }

        public static int usedInvertedMasks = 0;
        public static void AddInvertedMask(GameObject invertedMask, int maskBitIndex)
        {
            usedInvertedMasks |= 1 << maskBitIndex;
            invertedMasks.Add(invertedMask);
            Debug.Log("used masks: " + usedInvertedMasks);
            Shader.SetGlobalInt("_UsedMaskChannels", usedInvertedMasks); //todo
        }

        public static void RemoveInvertedMask(GameObject invertedMask, int maskBitIndex)
        {
            usedInvertedMasks &= ~(1 << maskBitIndex);
            invertedMasks.Remove(invertedMask);
            Debug.Log("used inv masks: " + usedInvertedMasks);
            Shader.SetGlobalInt("_UsedMaskChannels", usedInvertedMasks); //todo
        }

        public static void UpdateActiveMaskChannels(List<int> availableMaskChannels)
        {
            // var bitmask = GetUsedMaskChannelsBitmask(availableMaskChannels);
            // Debug.Log("used masks: " + bitmask);
            // Shader.SetGlobalInt("_UsedMaskChannels", bitmask); //todo
        }
        
        private static int GetUsedMaskChannelsBitmask(List<int> unusedMaskChannels)
        {
            int bitmask = 0xFFFFFF; // Start with all bits set
            foreach (int index in unusedMaskChannels)
            {
                bitmask &= ~(1 << index); // Clear the bit if the number is in the list
            }

            return bitmask & 0x7FFFFF; // Ensure only 23 bits are used
        }
    }
}