using System;
using System.Collections.Generic;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [RequireComponent(typeof(LayerGameObject))]
    public class MaskableVisualization : MonoBehaviour, IVisualizationWithPropertyData
    {
        private MaskingLayerPropertyData maskingLayerPropertyData;

        protected virtual void OnEnable()
        {
            // layerGameObject.onLayerReady.AddListener(RegisterListeners);
            PolygonSelectionLayerPropertyData.MaskDestroyed.AddListener(ResetMask);
        }

        protected virtual void OnDisable()
        {
            // layerGameObject.onLayerReady.RemoveListener(RegisterListeners);
            PolygonSelectionLayerPropertyData.MaskDestroyed.RemoveListener(ResetMask);
        }

        private void OnDestroy()
        {
            maskingLayerPropertyData.OnStylingChanged.RemoveListener(ApplyMasking);
        }

        private void ResetMask(int maskBitIndex)
        {
            maskingLayerPropertyData.SetMaskBit(maskBitIndex, true); //reset accepting masks
        }

        private void ApplyMasking()
        {
            var bitMask = GetBitMask();
            UpdateMaskBitMask(bitMask);
        }

        private int GetBitMask()
        {
            if (maskingLayerPropertyData == null) return MaskingLayerPropertyData.DEFAULT_MASK_BIT_MASK;

            int? bitMask = maskingLayerPropertyData.DefaultSymbolizer.GetMaskLayerMask();
            if (bitMask == null)
                bitMask = MaskingLayerPropertyData.DEFAULT_MASK_BIT_MASK;

            return bitMask.Value;
        }

        protected virtual void UpdateMaskBitMask(int bitmask)
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                UpdateBitMaskForMaterials(bitmask, r.materials);
            }

            //todo: this currently only works for regular objects and carteseian tile layers, CityJSON and 3dTiles should still be done, maybe make inherited classes for each to reduce coupling?
            var binaryMeshLayer = GetComponent<BinaryMeshLayer>();
            if (binaryMeshLayer != null)
                UpdateBitMaskForMaterials(bitmask, binaryMeshLayer.DefaultMaterialList);
        }

        protected void UpdateBitMaskForMaterials(int bitmask, IEnumerable<Material> materials)
        {
            foreach (var m in materials)
            {
                m.SetFloat("_MaskingChannelBitmask", bitmask);
            }
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            GetComponent<LayerGameObject>().InitProperty<MaskingLayerPropertyData>(properties);
            maskingLayerPropertyData = properties.Get<MaskingLayerPropertyData>();
            ApplyMasking();
            maskingLayerPropertyData.OnStylingChanged.AddListener(ApplyMasking);
        }
    }
}