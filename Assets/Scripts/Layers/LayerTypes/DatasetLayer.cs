using System.Collections.Generic;
using Netherlands3D.SubObjects;

namespace Netherlands3D.Twin.Layers
{
    public class DatasetLayer : ReferencedLayer
    {
        public ColorSetLayer ColorSetLayer { get; private set; } = new ColorSetLayer(0, new());

        private void Start()
        {
            RecalculateColorPriorities();
        }

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            base.OnLayerActiveInHierarchyChanged(isActive);
            ColorSetLayer.Enabled = isActive;
            GeometryColorizer.RecalculatePrioritizedColors();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
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
            var hierarchyList = GetFlatHierarchy(ReferencedProxy.Root);
            for (var i = 0; i < hierarchyList.Count; i++)
            {
                var datasetLayer = hierarchyList[i];
                datasetLayer.ColorSetLayer.PriorityIndex = i;
            }

            GeometryColorizer.RecalculatePrioritizedColors();
        }

        private List<DatasetLayer> GetFlatHierarchy(LayerNL3DBase root)
        {
            var list = new List<DatasetLayer>();

            AddLayersRecursive(root, list);

            return list;
        }

        private void AddLayersRecursive(LayerNL3DBase layer, List<DatasetLayer> list)
        {
            if (layer is ReferencedProxyLayer proxyLayer)
            {
                if (proxyLayer.Reference is DatasetLayer datasetLayer)
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