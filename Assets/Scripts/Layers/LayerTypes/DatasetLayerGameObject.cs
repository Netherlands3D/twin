using System.Collections.Generic;
using Netherlands3D.SubObjects;

namespace Netherlands3D.Twin.Layers
{
    public class DatasetLayerGameObject : LayerGameObject
    {
        public ColorSetLayer ColorSetLayer { get; private set; } = new ColorSetLayer(0, new());

        protected override void Start()
        {
            base.Start();
            RecalculateColorPriorities();
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
            // bool active = newColorSetLayer.Enabled;
            ColorSetLayer = newColorSetLayer;
            if (updateColors)
                GeometryColorizer.RecalculatePrioritizedColors();
        }

        public int PriorityIndex
        {
            get { return transform.GetSiblingIndex(); }
            set { transform.SetSiblingIndex(value); }
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

        private List<DatasetLayerGameObject> GetFlatHierarchy(LayerData root)
        {
            var list = new List<DatasetLayerGameObject>();

            AddLayersRecursive(root, list);

            return list;
        }

        private void AddLayersRecursive(LayerData layer, List<DatasetLayerGameObject> list)
        {
            if (layer is ReferencedLayerData proxyLayer)
            {
                if (proxyLayer.Reference is DatasetLayerGameObject datasetLayer)
                {
                    list.Add(datasetLayer);
                }
            }

            foreach (var child in layer.ChildrenLayers)
            {
                AddLayersRecursive(child, list);
            }
        }
    }
}