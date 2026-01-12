using Netherlands3D.CartesianTiles;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [RequireComponent(typeof(BinaryMeshLayer))]
    public class MaskableCartesianTileVisualization : MaskableVisualization
    {
        protected override void UpdateMaskBitMask(int bitmask)
        {
            var binaryMeshLayer = GetComponent<BinaryMeshLayer>();
            UpdateBitMaskForMaterials(bitmask, binaryMeshLayer.DefaultMaterialList);
        }
    }
}