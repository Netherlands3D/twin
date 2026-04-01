using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(PolygonSelectionLayerPropertyData))]
    public partial class PolygonMaskingPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private PolygonSelectionLayerPropertyData polygonPropertyData;
        
        private Toggle maskToggle;
        private Toggle maskInvertToggle;
        
        private MaskingPanel maskingPanel;
        private ContentContainer contentContainer;
        
        public PolygonMaskingPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            maskToggle = this.Q<Toggle>("IsMaskToggle");
            maskInvertToggle = this.Q<Toggle>("InvertMaskToggle");
            contentContainer = this.Q<ContentContainer>();

            
            maskToggle.RegisterValueChangedCallback(OnIsMaskChanged);
            maskInvertToggle.RegisterValueChangedCallback(OnInvertMaskChanged);
        }
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            polygonPropertyData = properties.Get<PolygonSelectionLayerPropertyData>();
            maskToggle.SetValueWithoutNotify(polygonPropertyData.IsMask);
            maskInvertToggle.SetValueWithoutNotify(polygonPropertyData.InvertMask);

            maskToggle.SetEnabled(maskToggle.value || PolygonSelectionLayerPropertyData.NumAvailableMasks > 0);
            SetMaxMasksText();

            if (polygonPropertyData.IsMask)
                AddMaskingPanel();
        }

        private void OnIsMaskChanged(ChangeEvent<bool> evt)
        {
            polygonPropertyData.IsMask = evt.newValue;
            
            if (evt.newValue)
                AddMaskingPanel();
            else
                RemoveMaskingPanel();
        
            SetMaxMasksText();
        }

        private void AddMaskingPanel()
        {
            var hierarchy = ProjectData.Current.RootLayer.GetFlatHierarchy();
            var layers =hierarchy.Where(layer => layer.GetProperty<MaskingLayerPropertyData>() != null).ToList(); //keep only the maskable layers
            maskingPanel = new MaskingPanel(layers, polygonPropertyData.MaskBitIndex);
            contentContainer.Add(maskingPanel);
        }
        
        private void RemoveMaskingPanel()
        {
            contentContainer.Remove(maskingPanel);
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