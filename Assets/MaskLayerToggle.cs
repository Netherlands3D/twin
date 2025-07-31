using System;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers
{
    public class MaskLayerToggle : MonoBehaviour
    {
        public PolygonSelectionLayer MaskLayer { get; set; }
        
        private LayerData layerData;
        public LayerData LayerData
        {
            get => layerData;
            set
            {
                layerData = value;
                toggle.interactable = value is ReferencedLayerData;
            }
        }

        private Toggle toggle;
        [SerializeField] private TextMeshProUGUI layerNameLabel;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();
            toggle.interactable = false;
        }

        public void Initialize(PolygonSelectionLayer mask, LayerData layer)
        {
            MaskLayer = mask;
            LayerData = layer;

            layerNameLabel.text = LayerData.Name;
        }

        private void OnEnable()
        {
            toggle.onValueChanged.AddListener(OnValueChanged);
        }
        
        private void OnDisable()
        {
            toggle.onValueChanged.RemoveListener(OnValueChanged);
        }

        private void OnValueChanged(bool acceptMask)
        {
            if (layerData is ReferencedLayerData referencedLayerData)
            {
                var currentLayerMask = LayerStyler.GetMaskLayerMask(referencedLayerData.Reference);
                int maskBitToSet = 1 << MaskLayer.MaskBitIndex;
                
                if (acceptMask)
                {
                    currentLayerMask |= maskBitToSet; // set bit to 1
                }
                else
                {
                    currentLayerMask &= ~maskBitToSet; // set bit to 0
                }

                LayerStyler.SetMaskLayerMask(referencedLayerData.Reference, currentLayerMask);
            }
        }
    }
}