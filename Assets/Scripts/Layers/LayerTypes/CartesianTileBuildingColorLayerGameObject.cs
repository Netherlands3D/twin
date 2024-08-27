using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers
{
    public class CartesianTileBuildingColorLayerGameObject : LayerGameObject, ILayerWithPropertyData
    {
        public int PriorityIndex
        {
            get { return transform.GetSiblingIndex(); }
            set { transform.SetSiblingIndex(value); }
        }

        public ColorSetLayer ColorSetLayer { get; private set; } = new(0, new());
        private CartesianTileBuildingColorPropertyData propertyData = new();
        public LayerPropertyData PropertyData => propertyData;

        public UnityEvent<float> progressEvent = new();

        protected override void Start()
        {
            base.Start();
            RecalculateColorPriorities();
            StartCoroutine(ReadAsync(propertyData.Data, 100));
        }
        
        private IEnumerator ReadAsync(Uri uri, int maxParsesPerFrame)
        {
            var csv = new CartesianTileBuildingColorCsv(uri, maxParsesPerFrame);

            // Wait a frame for the created layer to be re-parented and set up correctly to ensure the correct priority index
            yield return null;
            
            yield return csv.ReadAsync(UpdateProgress, SetColors);
        }

        private void UpdateProgress(float value)
        {
            progressEvent.Invoke(value);
        }

        private void SetColors(Dictionary<string, Color> colors)
        {
            var cl = GeometryColorizer.AddAndMergeCustomColorSet(this.PriorityIndex, colors);
            this.SetColorSetLayer(cl, false);
        }

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            base.OnLayerActiveInHierarchyChanged(isActive);
            ColorSetLayer.Enabled = isActive;
            GeometryColorizer.RecalculatePrioritizedColors();
        }

        protected void OnDestroy()
        {
            RemoveCustomColorSet();
        }

        public void RemoveCustomColorSet()
        {
            if (ColorSetLayer != null)
                GeometryColorizer.RemoveCustomColorSet(ColorSetLayer);
        }

        public void SetColorSetLayer(ColorSetLayer newColorSetLayer, bool updateColors = true)
        {
            ColorSetLayer = newColorSetLayer;
            if (updateColors)
                GeometryColorizer.RecalculatePrioritizedColors();
        }

        public override void OnProxyTransformParentChanged()
        {
            base.OnProxyTransformParentChanged();
            RecalculateColorPriorities();
        }

        private void RecalculateColorPriorities()
        {
            var hierarchyList = GetFlatHierarchy(LayerData.Root);
            for (var i = 0; i < hierarchyList.Count; i++)
            {
                var datasetLayer = hierarchyList[i];
                datasetLayer.ColorSetLayer.PriorityIndex = i;
            }

            GeometryColorizer.RecalculatePrioritizedColors();
        }

        private List<CartesianTileBuildingColorLayerGameObject> GetFlatHierarchy(LayerData root)
        {
            var list = new List<CartesianTileBuildingColorLayerGameObject>();

            AddLayersRecursive(root, list);

            return list;
        }

        private void AddLayersRecursive(LayerData layer, List<CartesianTileBuildingColorLayerGameObject> list)
        {
            if (layer is ReferencedLayerData proxyLayer)
            {
                if (proxyLayer.Reference is CartesianTileBuildingColorLayerGameObject datasetLayer)
                {
                    list.Add(datasetLayer);
                }
            }

            foreach (var child in layer.ChildrenLayers)
            {
                AddLayersRecursive(child, list);
            }
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var propertyData = properties.OfType<CartesianTileBuildingColorPropertyData>().FirstOrDefault();
            if (propertyData == null) return;

            this.propertyData = propertyData;
        }
    }
}