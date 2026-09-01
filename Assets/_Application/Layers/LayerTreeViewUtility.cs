using System;
using System.Collections.Generic;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Layers
{
    public static class LayerTreeViewUtility
    {
        public static void ReleaseListsToPool(TreeView tree)
        {
            ReleaseListsToPool(tree?.itemsSource as List<TreeViewItemData<LayerData>>);
        }
        
        private static void ReleaseListsToPool(List<TreeViewItemData<LayerData>> items)
        {
            if (items == null) return;
    
            foreach (var item in items)
                ReleaseListsToPool(item.children as List<TreeViewItemData<LayerData>>);
    
            ListPool<TreeViewItemData<LayerData>>.Release(items);
        }

        public static List<TreeViewItemData<LayerData>> ToTreeViewItems(LayerData rootLayer, TreeView oldTree, Func<LayerData, bool> filter = null, bool keepEmptyBranches = true)
        {
            ReleaseListsToPool(oldTree);
            return BuildRecursive(rootLayer.ChildrenLayers, filter, keepEmptyBranches);
        }

        private static List<TreeViewItemData<LayerData>> BuildRecursive(List<LayerData> layers, Func<LayerData, bool> filter = null, bool keepEmptyBranches = true)
        {
            var result = ListPool<TreeViewItemData<LayerData>>.Get();
            if (layers == null) return result;

            foreach (var layer in layers)
            {
                var children = BuildRecursive(layer.ChildrenLayers, filter, keepEmptyBranches);
                bool include = true;
                if(filter != null)
                    include = filter(layer);
                
                var includeBranch = true;
                if(!keepEmptyBranches)
                    includeBranch = children.Count > 0;
                
                if (include || includeBranch)
                    result.Add(new TreeViewItemData<LayerData>(layer.RootId, layer, children.Count > 0 ? children : null
                    ));
            }

            return result;
        }
    }
}