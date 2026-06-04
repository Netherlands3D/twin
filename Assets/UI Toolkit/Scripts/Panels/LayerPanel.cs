using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using ScrollView = UnityEngine.UIElements.ScrollView;
using TreeView = Netherlands3D.UI.Components.TreeView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class LayerPanel : BaseInspectorContentPanel
    {
        private const string aboveTargetUSSClassName = "layer-list-view-item--reparent-above";
        private const string reparentTargetUSSClassName = "layer-list-view-item--reparent-target";
        private const string belowTargetUSSClassName = "layer-list-view-item--reparent-below";
        private const string toRootTargetUSSClassName = "layer-list-view-item--reparent-to-root";

        private TreeView treeView;
        private ScrollView scrollView;
        private const float scrollSpeed = 300f; // px/s

        private LayerData rootLayer;
        private LayerDragGhost dragGhost;

        private Vector2 panelDragPosition;
        private int siblingIndex;
        private LayerListViewItem hoveredItem;

        private enum DropMode
        {
            Above,
            Into,
            Below,
            ToRoot
        }

        private DropMode currentDropMode;

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

            scrollView = treeView.Q<ScrollView>();

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
            
            panelDragPosition = startPosition;

            dragGhost = new LayerDragGhost();
            Add(dragGhost);
            dragGhost.Initialize(panelDragPosition, source);
        }

        private void OnDraggingLayerItem(Vector2 delta, LayerListViewItem source)
        {
            panelDragPosition += new Vector2(0, delta.y);
            
            var aboveTree = panelDragPosition.y < treeView.worldBound.yMin;
            var belowTree = panelDragPosition.y > treeView.worldBound.yMax;
            var atTopEdge = aboveTree && !MoveScrollView(-scrollSpeed);
            var atBottomEdge = belowTree && !MoveScrollView(scrollSpeed);
            
            dragGhost.UpdatePosition(panelDragPosition);
            
            if (atTopEdge || atBottomEdge)
            {
                SetHoveredItem(GetClosestItem(panelDragPosition.y));
                siblingIndex = atTopEdge ? 0 : -1;
                currentDropMode = DropMode.ToRoot;
                hoveredItem.EnableInClassList(toRootTargetUSSClassName, true);
                return;
            }
            
            var hitElement = panel.Pick(panelDragPosition);
            var targetItem = hitElement as LayerListViewItem
                             ?? hitElement?.GetFirstAncestorOfType<LayerListViewItem>()
                             ?? GetClosestItem(panelDragPosition.y);
            
            SetHoveredItem(targetItem);
            var layer = hoveredItem.userData as LayerData;
            siblingIndex = layer.ParentLayer.ChildrenLayers.IndexOf(layer);
        }

        private bool MoveScrollView(float deltaY)
        {
            var currentScrollOffset = scrollView.scrollOffset;

            var scrollDelta = new Vector2(0, deltaY * Time.deltaTime);
            scrollView.scrollOffset += scrollDelta;
            var realChange = scrollView.scrollOffset - currentScrollOffset;
            
            return Mathf.Abs(realChange.y) > 0.01f;
        }

        private LayerListViewItem GetClosestItem(float worldY)
        {
            var viewportBounds = scrollView.contentViewport.worldBound;
            LayerListViewItem closest = null;
            float closestDistance = float.MaxValue;
            treeView.Query<LayerListViewItem>().ForEach(item =>
            {
                if (!viewportBounds.Overlaps(item.worldBound)) return; // skip pooled/offscreen items
                float distance = Mathf.Abs(item.worldBound.center.y - worldY);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = item;
                }
            });
            return closest;
        }

        private void SetHoveredItem(LayerListViewItem targetItem)
        {
            if (hoveredItem != targetItem)
            {
                if (hoveredItem != null)
                {
                    hoveredItem.EnableInClassList(aboveTargetUSSClassName, false);
                    hoveredItem.EnableInClassList(reparentTargetUSSClassName, false);
                    hoveredItem.EnableInClassList(belowTargetUSSClassName, false);
                    hoveredItem.EnableInClassList(toRootTargetUSSClassName, false);
                }

                hoveredItem = targetItem;
            }

            var worldTop = hoveredItem.LocalToWorld(Vector2.zero);
            float localY = panelDragPosition.y - worldTop.y;
            float normalizedY = localY / hoveredItem.layout.height;

            if (normalizedY < 0.25f)
                currentDropMode = DropMode.Above;
            else if (normalizedY > 0.75f)
                currentDropMode = DropMode.Below;
            else
                currentDropMode = DropMode.Into;

            hoveredItem.EnableInClassList(aboveTargetUSSClassName, currentDropMode == DropMode.Above);
            hoveredItem.EnableInClassList(reparentTargetUSSClassName, currentDropMode == DropMode.Into);
            hoveredItem.EnableInClassList(belowTargetUSSClassName, currentDropMode == DropMode.Below);
        }

        private void OnDraggingLayerItemEnded(Vector2 endPosition, LayerListViewItem source)
        {
            if (hoveredItem != null)
            {
                var selectedLayers = treeView.selectedItems.ToList(); //to list makes a copy and avoids a collectionmodified error
                selectedLayers.Reverse();

                switch (currentDropMode)
                {
                    case DropMode.Above:
                        var newParentAbove = (hoveredItem.userData as LayerData).ParentLayer;
                        ReparentToLayer(selectedLayers, newParentAbove, siblingIndex);
                        break;
                    case DropMode.Into:
                        var newParent = hoveredItem.userData as LayerData;
                        ReparentToLayer(selectedLayers, newParent, -1);
                        break;
                    case DropMode.Below:
                        var newParentBelow = (hoveredItem.userData as LayerData).ParentLayer;
                        ReparentToLayer(selectedLayers, newParentBelow, siblingIndex + 1);
                        break;
                    case DropMode.ToRoot:
                        ReparentToLayer(selectedLayers, rootLayer, siblingIndex);
                        break;
                }
            }

            hoveredItem.EnableInClassList(aboveTargetUSSClassName, false);
            hoveredItem.EnableInClassList(reparentTargetUSSClassName, false);
            hoveredItem.EnableInClassList(belowTargetUSSClassName, false);
            hoveredItem.EnableInClassList(toRootTargetUSSClassName, false);
            hoveredItem = null;

            dragGhost.RemoveFromHierarchy();
            dragGhost = null;
        }

        private void ReparentToLayer(List<object> selectedLayers, LayerData newParent, int newSiblingIndex)
        {
            foreach (LayerData selectedLayer in selectedLayers)
            {
                selectedLayer.SetParent(newParent, newSiblingIndex);
            }
        }
    }
}