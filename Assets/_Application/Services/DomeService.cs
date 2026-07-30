using Netherlands3D.Masking;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D
{
    [RequireComponent(typeof(MaskingDomeSpawner))]
    public class DomeService : MonoBehaviour
    {
        public MaskingDomeSpawner Spawner => spawner;
        public bool IsPointerOnDome => Spawner.IsPointerOnDome;
        
        private MaskingDomeSpawner spawner;

        private void Start()
        {
            spawner = GetComponent<MaskingDomeSpawner>();
            spawner.SetMaskingBitIndex(MaskingLayerPropertyData.MASKING_DOME_BIT_INDEX);
        }
    }
}
