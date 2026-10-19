using System.Collections.Generic;
using Netherlands3D.CityJson.Structure;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectInformation
{
    public class CityJSONMapping : IMapping
    {
        public object MappingObject => feature;
        public string Id => feature.Id;
        public LayerData LayerData => null;
        
        public CityJSONLayerGameObject VisualisationParent  { get { return visualisationParent; } }
        
        private CityObject feature;
        private List<Mesh> meshes;
        private BoundingBox boundingBox;
        private CityJSONLayerGameObject visualisationParent;

        public BoundingBox BoundingBox
        {
            get
            {
                if(boundingBox == null)
                    UpdateBoundingBox();
                return boundingBox;
            }
        }

        public void UpdateBoundingBox()
        {
            Vector3Double MinExtent = feature.MinExtent;
            Vector3Double MaxExtent = feature.MaxExtent;
            boundingBox = new BoundingBox(new Coordinate(feature.CoordinateSystem, MinExtent.x, MinExtent.y, MinExtent.z), new Coordinate(feature.CoordinateSystem, MaxExtent.x, MaxExtent.y, MaxExtent.z));    
        }
        
        public void SetCityJsonLayerParent(CityJSONLayerGameObject parentLayer)
        {
            visualisationParent = parentLayer;
        }

        public void SetFeature(CityObject feature)
        {
            this.feature = feature; 
        }

        public void SetMeshes(List<Mesh> meshes)
        {
            this.meshes = meshes;       
        }
        
        public void Select(string subId = null)
        {
            foreach (var mesh in meshes)
            {
                Color col = new Color(1, 0, 0, 0);
                List<Color> colors = new List<Color>();
                foreach (Vector3 v in mesh.vertices)
                    colors.Add(col);

                mesh.SetColors(colors);
            }
        }

        public void Deselect()
        {
            foreach (var mesh in meshes)
            {
                Color col = new Color(0, 0, 0, 0);
                List<Color> colors = new List<Color>();
                foreach (Vector3 v in mesh.vertices)
                    colors.Add(col);

                mesh.SetColors(colors);
            }
        }
    }
}
