using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Layers
{
    public static class LayerDataExtensions
    {
        private static int _idCounter;

        public static List<TreeViewItemData<LayerData>> ToTreeViewItems(this LayerData rootLayer)
        {
            _idCounter = 0;
            return BuildRecursive(rootLayer.ChildrenLayers);
        }

        private static List<TreeViewItemData<LayerData>> BuildRecursive(List<LayerData> layers)
        {
            var result = new List<TreeViewItemData<LayerData>>();
            if (layers == null) return result;

            foreach (var layer in layers)
            {
                var children = BuildRecursive(layer.ChildrenLayers);

                result.Add(new TreeViewItemData<LayerData>(
                    _idCounter++,
                    layer,
                    children.Count > 0 ? children : null
                ));
            }

            return result;
        }
    }
}