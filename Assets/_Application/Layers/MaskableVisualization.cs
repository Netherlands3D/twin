using System;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [RequireComponent(typeof(LayerGameObject))]
    public class MaskableVisualization : MonoBehaviour, IVisualizationWithPropertyData
    {
        protected LayerGameObject layerGameObject;
        private MaskingLayerPropertyData maskingLayerPropertyData;

        private void Awake()
        {
            layerGameObject = GetComponent<LayerGameObject>();
        }

        protected virtual void OnEnable()
        {
            PolygonSelectionLayerPropertyData.MaskDestroyed.AddListener(ResetMask);
            var importedObject = GetComponent<IImportedObject>();
            if (importedObject != null)
            {
                importedObject.ObjectVisualized.AddListener(OnImportedObjectVisualized);
            }
        }

        protected virtual void OnDisable()
        {
            PolygonSelectionLayerPropertyData.MaskDestroyed.RemoveListener(ResetMask);
            var importedObject = GetComponent<IImportedObject>();
            if (importedObject != null)
            {
                importedObject.ObjectVisualized.AddListener(OnImportedObjectVisualized);
            }
        }

        private void OnImportedObjectVisualized(GameObject go)
        {
            ApplyMasking();
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

        protected int GetBitMask()
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
            layerGameObject.InitProperty<MaskingLayerPropertyData>(properties);
            maskingLayerPropertyData = properties.Get<MaskingLayerPropertyData>();
            layerGameObject.ConvertOldStylingDataIntoProperty(properties, "default", maskingLayerPropertyData);
            
            ApplyMasking();
            maskingLayerPropertyData.OnStylingChanged.AddListener(ApplyMasking);
        }
    }
}