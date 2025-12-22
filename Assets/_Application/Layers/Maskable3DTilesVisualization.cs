using Netherlands3D.Functionalities.OGC3DTiles;
using Netherlands3D.Tiles3D;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [RequireComponent(typeof(Tile3DLayerGameObject))]
    public class Maskable3DTilesVisualization : MaskableVisualization
    {
        Tile3DLayerGameObject tile3DLayerGameObject => layerGameObject as Tile3DLayerGameObject;
        protected override void OnEnable()
        {
            base.OnEnable();
            tile3DLayerGameObject.TileSet.OnTileLoaded.AddListener(InitializeStyling);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            tile3DLayerGameObject.TileSet.OnTileLoaded.RemoveListener(InitializeStyling);
        }

        private void InitializeStyling(Content content)
        {
            MaskingLayerPropertyData maskingPropertyData = tile3DLayerGameObject.LayerData.GetProperty<MaskingLayerPropertyData>();
            var bitmask = maskingPropertyData.DefaultSymbolizer.GetMaskLayerMask();
            
            if (bitmask == null)
                bitmask = MaskingLayerPropertyData.DEFAULT_MASK_BIT_MASK;
            
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                UpdateBitMaskForMaterials(bitmask.Value, r.materials);
            }
        }
    }
}