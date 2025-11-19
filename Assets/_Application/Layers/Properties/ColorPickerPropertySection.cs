using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.UI.ColorPicker;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [PropertySection(typeof(FillColorPropertyData))] //TODO what about stroke color? fill color is default
    public class ColorPickerPropertySection : MonoBehaviour, IVisualizationWithPropertyData
    {
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private ColorWheel colorPicker;
        public UnityEvent<Color> PickedColor = new();

        FillColorPropertyData fillColorPropertyData;

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            fillColorPropertyData = properties.Find(p => p is FillColorPropertyData) as FillColorPropertyData;
            if (fillColorPropertyData == null) return;
            
            fillColorPropertyData.OnColorChanged.AddListener(UpdateColorFromLayer);
            UpdateColorFromLayer();
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
            if (layer) layer.LayerData.OnStylingApplied.AddListener(UpdateColorFromLayer);
        }

        private void OnDisable()
        {
            colorPicker.colorChanged.RemoveListener(OnPickedColor);
            if (layer) layer.LayerData.OnStylingApplied.RemoveListener(UpdateColorFromLayer);
        }

        public void PickColorWithoutNotify(Color color)
        {
            colorPicker.SetColorWithoutNotify(color);
        }

      

        public void OnPickedColor(Color color)
        {
            PickedColor.Invoke(color);
            layer.LayerData.OnStylingApplied.Invoke();
        }

        private void UpdateColorFromLayer()
        {
            //todo move this to hierarchical object 
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