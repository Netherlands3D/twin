using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using TreeView = Netherlands3D.UI.Components.TreeView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class LayerPanel : BaseInspectorContentPanel
    {
        private TreeView treeView;
        private LayerData rootLayer;
        private LayerDragGhost dragGhost;
        
        private Vector2 panelDragPosition;
        private int siblingIndex;
        private LayerListViewItem hoveredItem;
        
        public LayerPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            treeView = this.Q<TreeView>();
            treeView.autoExpand = true;
            
            treeView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            treeView.selectionType = SelectionType.Multiple;
        
            treeView.makeItem = MakeItem;
            treeView.bindItem = BindItem;
            
            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Delete)
            {
                DeleteSelectedLayers();
            }
        }

        private void DeleteSelectedLayers()
        {
            foreach (LayerData layer in treeView.selectedItems.ToList()) //to list makes a copy and avoids a collectionmodified error
            {
                App.Layers.Remove(layer);
            }
        }

        private void RebuildTree()
        {
            PopulateLayerPanel(rootLayer);
        }
        
        public void PopulateLayerPanel(LayerData rootLayer)
        {
            this.rootLayer = rootLayer;
            var tree = LayerTreeViewUtility.ToTreeViewItems(rootLayer, treeView);
            treeView.SetRootItems(tree);
            treeView.RefreshItems();
        }

        private VisualElement MakeItem()
        {
            var layerRowElement = new LayerListViewItem();
            layerRowElement.RequestTreeRefresh.AddListener(treeView.RefreshItems);
            layerRowElement.RequestTreeRebuild.AddListener(RebuildTree);
            layerRowElement.DragStarted.AddListener(OnDraggingLayerItemStarted);
            layerRowElement.Dragging.AddListener(OnDraggingLayerItem);
            layerRowElement.DragEnded.AddListener(OnDraggingLayerItemEnded);
            return layerRowElement;
        }

        private void BindItem(VisualElement item, int index)
        {
            if (item is not LayerListViewItem layerRowElement) return;
    
            var layerData = treeView.GetItemDataForIndex<LayerData>(index);
            layerRowElement.Initialize(layerData);
        }

        public override string Title => "Lagen";
        
        private void OnDraggingLayerItemStarted(Vector2 startPosition, LayerListViewItem source)
        {
            if (dragGhost != null)
                dragGhost.RemoveFromHierarchy();
         
            
            var worldPosition = source.LocalToWorld(startPosition); // layer item to world
            var localPosition = this.WorldToLocal(worldPosition); // world to LayerPanel
            
            panelDragPosition = worldPosition;
            
            dragGhost = new LayerDragGhost();
            Add(dragGhost);
            dragGhost.Initialize(localPosition, source);
        }
        
        private void OnDraggingLayerItem(Vector2 delta, LayerListViewItem source)
        {
            dragGhost.UpdatePosition(delta);
            panelDragPosition += delta;
            
            var hitElement = panel.Pick(panelDragPosition);
            var targetItem = hitElement?.GetFirstAncestorOfType<LayerListViewItem>();
            if(targetItem == null)
                return;
            
            if(hoveredItem != targetItem)
            {
                if(hoveredItem != null)
                    hoveredItem.style.backgroundColor = StyleKeyword.Null;
                hoveredItem = targetItem;
                hoveredItem.style.backgroundColor = Color.red;
            }
            
            var layer = hoveredItem.userData as LayerData;
            siblingIndex = layer.ParentLayer.ChildrenLayers.IndexOf(layer);
        }

        private void OnDraggingLayerItemEnded(Vector2 endPosition, LayerListViewItem source)
        {
            var panelPosition = source.LocalToWorld(endPosition);
            
            var hitElement = panel.Pick(panelPosition);
            var targetItem = hitElement?.GetFirstAncestorOfType<LayerListViewItem>();
            
            if (targetItem != null)
            {
                var newParent = targetItem.userData as LayerData;

                var selectedLayers = treeView.selectedItems.ToList();
                selectedLayers.Reverse();
                foreach (LayerData selectedLayer in selectedLayers) //to list makes a copy and avoids a collectionmodified error
                {
                    selectedLayer.SetParent(newParent, siblingIndex); 
                }
            }

            dragGhost.RemoveFromHierarchy();
            dragGhost = null;
        }
    }
}