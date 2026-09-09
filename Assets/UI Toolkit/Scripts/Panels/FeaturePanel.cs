using System.Collections.Generic;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class FeaturePanel : VisualElement
    {
        public UnityEvent OnClose = new();
        private Button convert;
        
        private Dictionary<string, IMapping> mappings;

        public FeaturePanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            convert = this.Q<Button>("Convert");
            convert.clicked += OnConvert;
        }
        
        public FeaturePanel(Dictionary<string, IMapping> data) :  this()
        {
            mappings = data;

            convert.clicked += OnClose.Invoke;
            
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }
        
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            convert.clicked -= OnClose.Invoke;
        }

        private void OnConvert()
        {
            //todo support multiple polygons for creation?
            PolygonCreationService polygonCreationService = ServiceLocator.GetService<PolygonCreationService>();
            foreach (KeyValuePair<string, IMapping> mapping in mappings)
            {
                if (mapping.Value is FeatureMapping featureMapping)
                {
                    polygonCreationService.ConvertToLayer(featureMapping.Feature);
                    break;
                }
            }
        }
    }
}