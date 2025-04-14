using UnityEngine;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Twin.Utility;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using System.Linq;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles
{
    [RequireComponent(typeof(Layer))]
    public class CartesianTileLayerGameObject : LayerGameObject, ILayerWithPropertyPanels
    {
        public override BoundingBox Bounds => StandardBoundingBoxes.RDBounds; //assume we cover the entire RD bounds area
        
        private Layer layer;
        private Netherlands3D.CartesianTiles.TileHandler tileHandler;

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            if (layer && layer.isEnabled != isActive)
                layer.isEnabled = isActive;
        }
        
        protected virtual void Awake()
        {
            tileHandler = FindAnyObjectByType<Netherlands3D.CartesianTiles.TileHandler>();
            transform.SetParent(tileHandler.transform);
            layer = GetComponent<Layer>();

            tileHandler.AddLayer(layer);
        }

        protected virtual void OnDestroy()
        {
            if(Application.isPlaying && tileHandler && layer)
                tileHandler.RemoveLayer(layer);
        }

        private List<IPropertySectionInstantiator> propertySections;

        protected List<IPropertySectionInstantiator> PropertySections
        {
            get
            {
                if (propertySections == null)
                {
                    propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
                }

                return propertySections;
            }
            set => propertySections = value;
        }

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            return PropertySections;
        }
    }
}