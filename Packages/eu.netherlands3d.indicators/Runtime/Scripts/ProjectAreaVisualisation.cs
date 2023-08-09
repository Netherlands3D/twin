using System.Collections.Generic;
using GeoJSON.Net.Feature;
using Netherlands3D.Indicators.Dossiers;
using Netherlands3D.SelectionTools;

namespace Netherlands3D.Indicators
{
    public class ProjectAreaVisualisation
    {
        public readonly ProjectArea ProjectArea;
        public readonly Feature Feature; 
        public readonly List<PolygonVisualisation> Polygons;

        public ProjectAreaVisualisation(ProjectArea projectArea, Feature feature, List<PolygonVisualisation> polygons)
        {
            this.ProjectArea = projectArea;
            this.Feature = feature;
            this.Polygons = polygons;
        }
    }
}