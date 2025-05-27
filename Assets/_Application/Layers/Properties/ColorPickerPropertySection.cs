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
        private LayerGameObject layer;
        public UnityEvent<Color> PickedColor = new();

        public override LayerGameObject LayerGameObject
        {
            get => layer;
            set
            {
                layer = value;
                colorPicker.SetColorWithoutNotify(layer.LayerData.DefaultStyle.AnyFeature.Symbolizer.GetFillColor().GetValueOrDefault(defaultColor));
            }
        }

        private void Awake()
        {
            PickedColor.AddListener(OnPickDefaultFillColor);
        }

        private void OnDestroy()
        {
            PickedColor.RemoveListener(OnPickDefaultFillColor);
        }

        public void SetColorPickerColor(Color color)
        {
            colorPicker.SetColorWithoutNotify(color);
        }

        private void OnEnable()
        {
            colorPicker.colorChanged.AddListener(OnPickedColor);
        }

        private void OnDisable()
        {
            colorPicker.colorChanged.RemoveListener(OnPickedColor);
        }

        public void OnPickedColor(Color color)
        {
            PickedColor.Invoke(color);
            layer.ApplyStyling();
        }

        /// <summary>
        /// Default behaviour is to set the picked color as the, unconditional, fill color for the given layer. This
        /// should suffice in most situations, but if a different behaviour is desired you can remove all listeners
        /// from the PickedColor event and provide your own implementation.
        /// </summary>
        /// <param name="color"></param>
        private void OnPickDefaultFillColor(Color color)
        {
            if (layer is HierarchicalObjectLayerGameObject hierarchicalObjectLayerGameObject)
            {
                HierarchicalObjectTileLayerStyler.SetColor(hierarchicalObjectLayerGameObject, color);
            }
        }
    }
}