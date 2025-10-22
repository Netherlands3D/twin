using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.UI.ColorPicker;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class ColorPickerPropertySection : PropertySectionWithLayerGameObject
    {
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private ColorWheel colorPicker;
        public UnityEvent<Color> PickedColor = new();

        private LayerGameObject layer;

        public override LayerGameObject LayerGameObject
        {
            get => layer;
            set => ChangeLayerTo(value);
        }

        private void Awake()
        {
            PickedColor.AddListener(OnColorPicked);
        }

        private void OnDestroy()
        {
            PickedColor.RemoveListener(OnColorPicked);
        }

        private void OnEnable()
        {
            colorPicker.colorChanged.AddListener(OnPickedColor);
            if (layer) layer.OnStylingApplied.AddListener(UpdateColorFromLayer);
        }

        private void OnDisable()
        {
            colorPicker.colorChanged.RemoveListener(OnPickedColor);
            if (layer) layer.OnStylingApplied.RemoveListener(UpdateColorFromLayer);
        }

        public void PickColorWithoutNotify(Color color)
        {
            colorPicker.SetColorWithoutNotify(color);
        }

        /// <summary>
        /// Since the layer may or may not be known on Awake/Start of this component, we need to remove and re-add
        /// listeners on a layer when we set/replace it, and update the color in the color picker to that of the layer.
        /// </summary>
        private void ChangeLayerTo(LayerGameObject value)
        {
            if (layer && enabled) layer.OnStylingApplied.RemoveListener(UpdateColorFromLayer);
            layer = value;
            if (layer && enabled) layer.OnStylingApplied.AddListener(UpdateColorFromLayer);

            UpdateColorFromLayer();
        }

        public void OnPickedColor(Color color)
        {
            PickedColor.Invoke(color);
            layer.ApplyStyling();
        }

        private void UpdateColorFromLayer()
        {
            if (layer is HierarchicalObjectLayerGameObject hierarchicalObjectLayerGameObject)
            {
                var color = HierarchicalObjectLayerStyler.GetColor(hierarchicalObjectLayerGameObject);
                this.PickColorWithoutNotify(color.HasValue ? color.Value : defaultColor);
            }
        }

        /// <summary>
        /// Default behaviour is to set the picked color as the, unconditional, fill color for the given layer. This
        /// should suffice in most situations, but if a different behaviour is desired you can remove all listeners
        /// from the PickedColor event and provide your own implementation.
        /// </summary>
        /// <param name="color"></param>
        private void OnColorPicked(Color color)
        {
            if (layer is HierarchicalObjectLayerGameObject hierarchicalObjectLayerGameObject)
            {
                HierarchicalObjectLayerStyler.SetColor(hierarchicalObjectLayerGameObject, color);
            }
        }
    }
}