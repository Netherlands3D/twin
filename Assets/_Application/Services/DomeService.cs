using System;
using Netherlands3D.Masking;
using Netherlands3D.Services;
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

        private void Awake()
        {
            spawner = GetComponent<MaskingDomeSpawner>();
        }

        private void Start()
        {
            spawner.SetMaskingBitIndex(MaskingLayerPropertyData.MASKING_DOME_BIT_INDEX);
            DisableDome();
        }

        private void OnEnable()
        {
            ToolService toolService = ServiceLocator.GetService<ToolService>();
            toolService.GetTool(ToolType.Dome).onOpen.AddListener(EnableDome);
            toolService.GetTool(ToolType.Dome).onClose.AddListener(DisableDome);
        }

        private void OnDisable()
        {
            ToolService toolService = ServiceLocator.GetService<ToolService>();
            toolService.GetTool(ToolType.Dome).onOpen.RemoveListener(EnableDome);
            toolService.GetTool(ToolType.Dome).onClose.RemoveListener(DisableDome);
        }

        private void EnableDome()
        {
            spawner.SetDomeEnabled();
        }

        private void DisableDome()
        {
            Spawner.SetDomeDisabled();
        }
    }
}
