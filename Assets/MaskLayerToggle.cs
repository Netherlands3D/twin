using Netherlands3D.Twin.Layers.Properties;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties
{
    public class MaskLayerToggle : MonoBehaviour
    {
        private MaskingLayerPropertyData layerPropertyData;

        public MaskingLayerPropertyData LayerPropertyData
        {
            get => layerPropertyData;
            set
            {
                layerPropertyData = value;
                toggle.interactable = layerPropertyData != null;
            }
        }

        [SerializeField] private Toggle toggle;
        [SerializeField] private TextMeshProUGUI layerNameLabel;
        [SerializeField] private Image layerIconImage;
        [SerializeField] private Image maskIconImage;
        [SerializeField] private LayerTypeSpriteLibrary layerTypeSpriteLibrary;
        [SerializeField] private Color acceptMaskTextColor = new Color(204f / 255f, 215 / 255f, 228f / 255f);
        [SerializeField] private Color ignoreMaskTextColor = new Color(47f / 255f, 53f / 255f, 80f / 255f);

        private PolygonSelectionLayerPropertyData maskPropertyData;

        private void Awake()
        {
            toggle.interactable = false;
        }

        public void Initialize(PolygonSelectionLayerPropertyData maskPropertyData, LayerData layerToAffect)
        {
            this.maskPropertyData = maskPropertyData;
            LayerPropertyData = layerToAffect.GetProperty<MaskingLayerPropertyData>();
            if (LayerPropertyData == null)
                return;

            layerNameLabel.text = layerToAffect.Name;

            var layerTypeSpriteCollection = layerTypeSpriteLibrary.GetLayerTypeSprite(layerToAffect);
            layerIconImage.sprite = layerTypeSpriteCollection.PrimarySprite;

            var currentLayerMask = LayerPropertyData.GetMaskLayerMask();
            int maskBitToCheck = 1 << maskPropertyData.MaskBitIndex;
            bool isBitSet = (currentLayerMask & maskBitToCheck) != 0;
            toggle.SetIsOnWithoutNotify(!isBitSet);

            UpdateUIAppearance(isBitSet);
        }

        private void OnEnable()
        {
            toggle.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnDisable()
        {
            toggle.onValueChanged.RemoveListener(OnValueChanged);
        }

        private void OnValueChanged(bool isOn)
        {
            var acceptMask = !isOn;
            LayerPropertyData.SetMaskBit(maskPropertyData.MaskBitIndex, acceptMask);
            UpdateUIAppearance(acceptMask);
        }

        private void UpdateUIAppearance(bool acceptMask)
        {
            maskIconImage.gameObject.SetActive(acceptMask);
            layerNameLabel.color = acceptMask ? acceptMaskTextColor : ignoreMaskTextColor;
        }
    }
}