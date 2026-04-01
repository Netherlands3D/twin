using System;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(PolygonSelectionLayerPropertyData))]
    public partial class PolygonPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        const string inactiveUSSClassName = "inactive";
        
        private PolygonSelectionLayerPropertyData polygonPropertyData;
        
        private VisualElement linePropertiesElement;
        private Slider lineWidthSlider;
        
        private VisualElement gridPropertiesElement;
        private Button editGridButton;
        
        public PolygonPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            linePropertiesElement = this.Q<VisualElement>("LineProperties");
            lineWidthSlider = this.Q<Slider>("LineWidthSlider");
            
            gridPropertiesElement = this.Q<VisualElement>("GridProperties");
            editGridButton = this.Q<Button>("EditGridButton");
            
            lineWidthSlider.RegisterValueChangedCallback(OnStrokeWidthChanged);
            editGridButton.RegisterCallback<ClickEvent>(OnEditGridButtonPressed);
        }
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            polygonPropertyData = properties.Get<PolygonSelectionLayerPropertyData>();
            
            lineWidthSlider.SetValueWithoutNotify(polygonPropertyData.LineWidth);
            if (polygonPropertyData.ShapeType != ShapeType.Line && polygonPropertyData.ShapeType != ShapeType.Grid)
            {
                //We don't have any specific information to show, so we delete the panel again
                parent.Remove(this);
            }
            
            SetSectionVisible(linePropertiesElement, polygonPropertyData.ShapeType == ShapeType.Line);
            SetSectionVisible(gridPropertiesElement, polygonPropertyData.ShapeType == ShapeType.Grid);
        }

        private void SetSectionVisible(VisualElement section, bool isVisible)
        {
            if (isVisible)
                section.RemoveFromClassList(inactiveUSSClassName);
            else
                section.AddToClassList(inactiveUSSClassName);
        }

        private void OnStrokeWidthChanged(ChangeEvent<float> evt)
        {
            polygonPropertyData.LineWidth = evt.newValue;
        }
        
        private void OnEditGridButtonPressed(ClickEvent evt)
        {
            Debug.Log("edit grid button pressed");
            throw new NotImplementedException();
        }
    }
}