using UnityEngine;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Twin.Utility;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.LayerStyles;
using System;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles
{
    [RequireComponent(typeof(Layer))]
    public partial class CartesianTileLayerGameObject : LayerGameObject, ILayerWithPropertyPanels
    {
        public override BoundingBox Bounds => StandardBoundingBoxes.RDBounds; //assume we cover the entire RD bounds area
        
        private Layer layer;
        private Netherlands3D.CartesianTiles.TileHandler tileHandler;

        private CartesianTileBinaryMeshLayerMaterialApplicator applicator;
        internal CartesianTileBinaryMeshLayerMaterialApplicator Applicator
        {
            get
            {
                if (applicator == null)
                    applicator = new CartesianTileBinaryMeshLayerMaterialApplicator(this);
                return applicator;
            }
        }

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

        public override void ApplyStyling()
        {
            //MaterialApplicator.Apply(Applicator); using creatematerial directly because this is a sharedmaterial
            Applicator.CreateMaterial();

            base.ApplyStyling();
        }

        protected override LayerFeature AddAttributesToLayerFeature(LayerFeature feature)
        {
            if (feature.Geometry is not Material mat) return feature;

            BinaryMeshLayer meshLayer = layer as BinaryMeshLayer;
            feature.Attributes.Add("materialindex", meshLayer.DefaultMaterialList.IndexOf(mat).ToString());
            feature.Attributes.Add("materialname", mat.name);

            return feature;
        }

        private Material UpdateMaterial(Color color, int index)
        {
            if(layer is BinaryMeshLayer meshLayer)
            {
                string matName = meshLayer.DefaultMaterialList[index].name;
                meshLayer.DefaultMaterialList[index].color = color;
                return meshLayer.DefaultMaterialList[index];
            }
            throw new NotImplementedException();
        }

        private Material GetMaterialInstance(int index)
        {
            BinaryMeshLayer meshLayer = layer as BinaryMeshLayer;
            if (meshLayer)
            {
                return meshLayer.DefaultMaterialList[index];
            }
            throw new NotImplementedException();
        }
    }
}