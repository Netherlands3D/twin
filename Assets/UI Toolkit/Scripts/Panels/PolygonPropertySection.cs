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
        
        private Toggle maskToggle;
        private Toggle maskInvertToggle;
        
        public PolygonPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            linePropertiesElement = this.Q<VisualElement>("LineProperties");
            lineWidthSlider = this.Q<Slider>("LineWidthSlider");
            
            gridPropertiesElement = this.Q<VisualElement>("GridProperties");
            editGridButton = this.Q<Button>("EditGridButton");
            
            maskToggle = this.Q<Toggle>("IsMaskToggle");
            maskInvertToggle = this.Q<Toggle>("InvertMaskToggle");
            
            lineWidthSlider.RegisterValueChangedCallback(OnStrokeWidthChanged);
            editGridButton.RegisterCallback<ClickEvent>(OnEditGridButtonPressed);
            maskToggle.RegisterValueChangedCallback(OnIsMaskChanged);
            maskInvertToggle.RegisterValueChangedCallback(OnInvertMaskChanged);
        }
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            polygonPropertyData = properties.Get<PolygonSelectionLayerPropertyData>();
            
            lineWidthSlider.SetValueWithoutNotify(polygonPropertyData.LineWidth);
            maskToggle.SetValueWithoutNotify(polygonPropertyData.IsMask);
            maskInvertToggle.SetValueWithoutNotify(polygonPropertyData.InvertMask);
            
            SetSectionVisible(linePropertiesElement, polygonPropertyData.ShapeType == ShapeType.Line);
            SetSectionVisible(gridPropertiesElement, polygonPropertyData.ShapeType == ShapeType.Grid);

            maskToggle.SetEnabled(maskToggle.value || PolygonSelectionLayerPropertyData.NumAvailableMasks > 0);
            SetMaxMasksText();

            // if (polygonPropertyData.IsMask)
            //     PopulateMaskLayerPanel();
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

        private void OnIsMaskChanged(ChangeEvent<bool> evt)
        {
            polygonPropertyData.IsMask = evt.newValue;
            
            // if (isMask)
            //     PopulateMaskLayerPanel();
            // else
            //     ClearMaskLayerPanel();

            SetMaxMasksText();
        }
        
        private void OnInvertMaskChanged(ChangeEvent<bool> evt)
        {
            polygonPropertyData.InvertMask = evt.newValue;
        }
        
        private void SetMaxMasksText()
        {
            Debug.Log("set amount of available masks text");
            // maxMasksText.text = string.Format(maxMasksTextTemplate, PolygonSelectionLayerPropertyData.NumAvailableMasks.ToString(), PolygonSelectionLayerPropertyData.MaxAvailableMasks.ToString());
        }
    }
}