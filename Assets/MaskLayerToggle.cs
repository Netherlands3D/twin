using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.Twin.Projects;
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
                toggle.interactable = ProjectData.Current.PrefabLibrary.GetPrefabById(layerData.PrefabIdentifier) != null;
            }
        }

        [SerializeField] private Toggle toggle;
        [SerializeField] private TextMeshProUGUI layerNameLabel;
        [SerializeField] private Image layerIconImage;
        [SerializeField] private Image maskIconImage;
        [SerializeField] private LayerTypeSpriteLibrary layerTypeSpriteLibrary;
        [SerializeField] private Color acceptMaskTextColor = new Color(204f / 255f, 215 / 255f, 228f / 255f);
        [SerializeField] private Color ignoreMaskTextColor = new Color(47f / 255f, 53f / 255f, 80f / 255f);

        private void Awake()
        {
            toggle.interactable = false;
        }

        public void Initialize(PolygonSelectionLayer mask, LayerData layer)
        {
            MaskLayer = mask;
            LayerData = layer;

            layerNameLabel.text = LayerData.Name;

            var layerTypeSpriteCollection = layerTypeSpriteLibrary.GetLayerTypeSprite(layer);
            layerIconImage.sprite = layerTypeSpriteCollection.PrimarySprite; //initialize the sprite correctly in case it is not a ReferencedLayerData

            LayerGameObject template = ProjectData.Current.PrefabLibrary.GetPrefabById(layerData.PrefabIdentifier);
            if (template != null)
            {
                var currentLayerMask = template.GetMaskLayerMask(); //TODO check if this is global or instance unique?
                int maskBitToCheck = 1 << MaskLayer.MaskBitIndex;
                bool isBitSet = (currentLayerMask & maskBitToCheck) != 0;
                toggle.SetIsOnWithoutNotify(!isBitSet);
                UpdateUIAppearance(isBitSet);
            }
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
            layerData.Visualization?.SetMaskBit(MaskLayer.MaskBitIndex, acceptMask);
            UpdateUIAppearance(acceptMask);
        }

        private void UpdateUIAppearance(bool acceptMask)
        {
            maskIconImage.gameObject.SetActive(acceptMask);
            var layerTypeSpriteCollection = layerTypeSpriteLibrary.GetLayerTypeSprite(layerData);
            layerIconImage.sprite = acceptMask ? layerTypeSpriteCollection.SecondarySprite : layerTypeSpriteCollection.PrimarySprite;
            layerNameLabel.color = acceptMask ? acceptMaskTextColor : ignoreMaskTextColor;
        }
    }
}