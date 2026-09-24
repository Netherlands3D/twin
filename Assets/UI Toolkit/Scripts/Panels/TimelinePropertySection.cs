using System.Collections.Generic;
using Netherlands3D.Timeline;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(TimelineStylingLayerPropertyData), PropertySectionCategory.Styling)]
    public partial class TimelinePropertySection : VisualElement, IVisualizationWithPropertyData, IPropertyPanelWithColorPicker
    {
        private TimelineStylingLayerPropertyData timelineStylingPropertyData;
        private TimelineValueStateInterpreterPanel spawnedPanel; //todo make generic base class
        public ColorPicker ColorPicker { get; set; }

        private ContentContainer container;
        
        public TimelinePropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            container = this.Q<ContentContainer>();
        }
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            timelineStylingPropertyData = properties.Get<TimelineStylingLayerPropertyData>();

            if (timelineStylingPropertyData.Interpreter is TimestampValueStatusInterpreter valueStatusInterpreter)
            {
                spawnedPanel = new TimelineValueStateInterpreterPanel(valueStatusInterpreter, ColorPicker);
                container.Add(spawnedPanel);
            }
        }
    }
}