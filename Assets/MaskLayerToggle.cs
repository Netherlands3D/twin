using System;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.Twin.Layers.UI.HierarchyInspector;
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

        [SerializeField] private Toggle toggle;
        [SerializeField] private TextMeshProUGUI layerNameLabel;
        [SerializeField] private Image layerIconImage;
        [SerializeField] private LayerTypeSpriteLibrary layerTypeSpriteLibrary;
        
        private void Awake()
        {
            toggle.interactable = false;
        }

        public void Initialize(PolygonSelectionLayer mask, LayerData layerData)
        {
            MaskLayer = mask;
            LayerData = layerData;

            layerNameLabel.text = LayerData.Name;

            if (this.layerData is ReferencedLayerData referencedLayerData)
            {
                var visualization = layerData.RequestVisualization();
                var currentLayerMask = visualization.GetMaskLayerMask();
                int maskBitToCheck = 1 << MaskLayer.MaskBitIndex;
                bool isBitSet = (currentLayerMask & maskBitToCheck) != 0;
                toggle.SetIsOnWithoutNotify(isBitSet);

            }

            layerIconImage.sprite = layerTypeSpriteLibrary.GetLayerTypeSprite(layerData);
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
                referencedLayerData.Reference.SetMaskBit(MaskLayer.MaskBitIndex, acceptMask);
            }
        }
    }
}