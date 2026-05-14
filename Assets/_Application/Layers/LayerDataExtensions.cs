using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Layers
{
    public static class LayerDataExtensions
    {
        private static int _idCounter;

        public static List<TreeViewItemData<LayerData>> ToTreeViewItems(this LayerData rootLayer, Func<LayerData, bool> filter = null)
        {
            _idCounter = 0;
            return BuildRecursive(rootLayer.ChildrenLayers, filter);
        }

        private static List<TreeViewItemData<LayerData>> BuildRecursive(List<LayerData> layers, Func<LayerData, bool> filter = null)
        {
            var result = new List<TreeViewItemData<LayerData>>();
            if (layers == null) return result;

            foreach (var layer in layers)
            {
                var children = BuildRecursive(layer.ChildrenLayers, filter);
                bool include = true;
                if(filter != null)
                    include = filter(layer);

                if (include)
                    result.Add(new TreeViewItemData<LayerData>(_idCounter++, layer, children.Count > 0 ? children : null
                    ));
            }

            return result;
        }
    }
}