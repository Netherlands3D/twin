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
                if (applicator == null) applicator = new CartesianTileBinaryMeshLayerMaterialApplicator(this);

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
            // The color in the Layer Panel represents the default fill color for this layer
            LayerData.Color = LayerData.DefaultSymbolizer?.GetFillColor() ?? LayerData.Color;

            //MaterialApplicator.Apply(Applicator); using creatematerial directly because this is a sharedmaterial
            Applicator.CreateMaterial();

            base.ApplyStyling();
        }

        protected override LayerFeature AddAttributesToLayerFeature(LayerFeature feature)
        {
            // it should be a FeaturePolygonVisualisations, just do a sanity check here
            if (feature.Geometry is not Material mat) return feature;

            feature.Attributes.Clear();
            feature.Attributes.Add(mat.name, mat.name);

            return feature;
        }

        private Material UpdateMaterial(Color color, int index)
        {
            if(layer is BinaryMeshLayer meshLayer)
            {
                meshLayer.DefaultMaterialList[index].color = color;
                return meshLayer.DefaultMaterialList[index];
            }
            throw new NotImplementedException();
        }

        //private void SetMaterialInstance(Material materialInstance, int index)
        //{
        //    BinaryMeshLayer meshLayer = layer as BinaryMeshLayer;
        //    if (meshLayer)
        //    {
        //        meshLayer.DefaultMaterialList[index] = materialInstance;
        //    }
        //    else
        //        throw new NotImplementedException();
        //}

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