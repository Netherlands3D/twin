using System.Collections.Generic;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(ColorPropertyData))]
    public partial class ColorStylingPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private Color defaultColor = Color.white;
        private ColorPropertyData stylingPropertyData;
        private ColorPicker colorPicker;

        public ColorStylingPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            colorPicker = this.Q<ColorPicker>();
            
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            stylingPropertyData.OnStylingChanged.RemoveListener(UpdateColorFromProperty);
            colorPicker.ColorChanged.RemoveListener(OnColorPicked);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            stylingPropertyData = properties.GetDefaultStylingPropertyData<ColorPropertyData>();

            if (stylingPropertyData == null) return;
            
            stylingPropertyData.OnStylingChanged.AddListener(UpdateColorFromProperty);
            colorPicker.ColorChanged.AddListener(OnColorPicked);

            UpdateColorFromProperty();
        }

        private void UpdateColorFromProperty()
        {
            Color? color = stylingPropertyData.GetDefaultSymbolizerColor();
            colorPicker.SetColorInputComponentsWithoutNotify(color.HasValue ? color.Value : defaultColor);
        }

        private void OnColorPicked(Color color)
        {
            stylingPropertyData.SetDefaultSymbolizerColor(color);
        }
    }
}