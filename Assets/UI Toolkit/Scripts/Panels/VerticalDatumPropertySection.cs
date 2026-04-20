using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using RadioButtonGroup = UnityEngine.UIElements.RadioButtonGroup;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(ILayerPropertyDataWithCRS))]
    public partial class VerticalDatumPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private RadioButtonGroup referenceHeightOptions;
        private ILayerPropertyDataWithCRS propertyData;
       
        public VerticalDatumPropertySection()
        {            
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            referenceHeightOptions = this.Q<RadioButtonGroup>();
            referenceHeightOptions.RegisterValueChangedCallback(OnReferenceHeightChanged);
        }

        private void OnReferenceHeightChanged(ChangeEvent<int> evt)
        {
            switch (evt.newValue)
            {
                case 0:
                    propertyData.ContentCRS = (int)CoordinateSystem.WGS84_ECEF;
                    break;
                case 1:
                    propertyData.ContentCRS = (int)CoordinateSystem.WGS84_NAP_ECEF;
                    break;
            }
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            propertyData = properties.OfType<ILayerPropertyDataWithCRS>().FirstOrDefault();
            
            var usesEllipsoid = propertyData.ContentCRS == (int)CoordinateSystem.WGS84_ECEF;
            referenceHeightOptions.SetValueWithoutNotify(usesEllipsoid ? 0 : 1);
        }
    }
}