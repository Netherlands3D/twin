using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Layers
{
    public static class LayerDataExtensions
    {
        private static int _idCounter;
        private static List<TreeViewItemData<LayerData>> tempResult = new List<TreeViewItemData<LayerData>>();

        public static List<TreeViewItemData<LayerData>> ToTreeViewItems(this LayerData rootLayer, Func<LayerData, bool> filter = null, bool keepEmptyBranches = true)
        {
            _idCounter = 0;
            return BuildRecursive(rootLayer.ChildrenLayers, filter, keepEmptyBranches);
        }

        private static List<TreeViewItemData<LayerData>> BuildRecursive(List<LayerData> layers, Func<LayerData, bool> filter = null, bool keepEmptyBranches = true)
        {
            tempResult.Clear();
            if (layers == null) return tempResult;

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
                    tempResult.Add(new TreeViewItemData<LayerData>(_idCounter++, layer, children.Count > 0 ? children : null
                    ));
            }

            return tempResult;
        }
    }
}