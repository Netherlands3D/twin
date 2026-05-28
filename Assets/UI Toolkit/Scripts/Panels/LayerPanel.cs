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
            
            var panelPosition = source.LocalToWorld(startPosition);

            dragGhost = new LayerDragGhost();
            Add(dragGhost);
            dragGhost.Initialize(panelPosition, source);
        }

        private void OnDraggingLayerItem(Vector2 delta, LayerListViewItem source)
        {
            dragGhost.UpdatePosition(delta);
        }

        private void OnDraggingLayerItemEnded(Vector2 endPosition, LayerListViewItem source)
        {
            var panelPosition = source.LocalToWorld(endPosition);
            Debug.Log(panelPosition);
            
            var hitElement = panel.Pick(panelPosition);
            var targetItem = hitElement?.GetFirstAncestorOfType<LayerListViewItem>();
            
            if (targetItem != null)
            {
                var layer = targetItem.userData as LayerData;
                Debug.Log(layer == null);
                Debug.Log(layer.Name);
            }

            dragGhost.RemoveFromHierarchy();
            dragGhost = null;
        }
    }
}