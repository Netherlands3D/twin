using System.Collections.Generic;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(BuildingPropertyData))]
    public partial class BuildingInformationPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private const string idReplacementString = "{BagID}";
        private const string bagRequestUrl = "https://service.pdok.nl/lv/bag/wfs/v2_0?SERVICE=WFS&VERSION=2.0.0&outputFormat=geojson&REQUEST=GetFeature&typeName=bag:pand&count=100&outputFormat=xml&srsName=EPSG:28992&filter=%3cFilter%3e%3cPropertyIsEqualTo%3e%3cPropertyName%3eidentificatie%3c/PropertyName%3e%3cLiteral%3e{BagID}%3c/Literal%3e%3c/PropertyIsEqualTo%3e%3c/Filter%3e";
        private const string addressRequestUrl = "https://service.pdok.nl/lv/bag/wfs/v2_0?service=wfs&request=getFeature&version=2.0.0&outputFormat=geojson&typeName=bag:verblijfsobject&filter=%3CFilter%3E%3CPropertyIsEqualTo%3E%3CPropertyName%3Epandidentificatie%3C/PropertyName%3E%3CLiteral%3E{BagID}%3C/Literal%3E%3C/PropertyIsEqualTo%3E%3C/Filter%3E";
        private const string removeFromID = "NL.IMBAG.Pand.";
        
        private BuildingPropertyData buildingPropertyData;

        public BuildingInformationPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                buildingPropertyData.OnIdsChanged.RemoveListener(OnIdsChanged);
            });
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            buildingPropertyData = properties.Get<BuildingPropertyData>();
            buildingPropertyData.OnIdsChanged.AddListener(OnIdsChanged);
        }

        private void OnIdsChanged(List<string> buildingIds)
        {
            
        }
    }
}