using System;
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
        private const float scrollZoneSize = 50f;
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

            // var worldPosition = source.LocalToWorld(startPosition); // layer item to world
            // var localPosition = this.WorldToLocal(worldPosition); // world to LayerPanel

            panelDragPosition = startPosition;
            // var localPosition = this.WorldToLocal(startPosition);
            
            dragGhost = new LayerDragGhost();
            // panel.visualTree.Add(dragGhost);
            Add(dragGhost);
            dragGhost.Initialize(panelDragPosition, source);
        }

        private void OnDraggingLayerItem(Vector2 delta, LayerListViewItem source)
        {
            // var worldPosition = source.LocalToWorld(startPosition); // layer item to world
            // var localPosition = this.WorldToLocal(worldPosition); // world to LayerPanel
            //
            // panelDragPosition = worldPosition;
            
            panelDragPosition += new Vector2(0, delta.y);
            dragGhost.UpdatePosition(panelDragPosition);

            var hitElement = panel.Pick(panelDragPosition);
            var targetItem = hitElement as LayerListViewItem ?? hitElement?.GetFirstAncestorOfType<LayerListViewItem>();
            // Debug.Log("hit: "  + hitElement?.name + "\ttarget: " + targetItem?.name);
            if (targetItem == null)
            {
                var listViewItems = treeView.Query<LayerListViewItem>();
                if (panelDragPosition.y < treeView.worldBound.yMin)
                {
                    if(MoveScrollView(-scrollSpeed))
                    {
                        dragGhost.UpdatePosition(panelDragPosition);
                        return;
                    }
                    
                    targetItem = listViewItems.First();
                    siblingIndex = 0;
                }
                else if (panelDragPosition.y > treeView.worldBound.yMax)
                {
                    if (MoveScrollView(scrollSpeed))
                    {
                        dragGhost.UpdatePosition(panelDragPosition);
                        return;
                    }
                    
                    targetItem = listViewItems.Last();
                    siblingIndex = -1; // -1 means it will be added to the bottom of the list, which is what we want in this case
                }
                
                SetHoveredItem(targetItem);
                currentDropMode = DropMode.ToRoot;
                hoveredItem.EnableInClassList(toRootTargetUSSClassName, currentDropMode == DropMode.ToRoot);
                
                return;
            }
            
            SetHoveredItem(targetItem);

            var layer = hoveredItem.userData as LayerData;
            siblingIndex = layer.ParentLayer.ChildrenLayers.IndexOf(layer);
        }

        private bool MoveScrollView(float deltaY)
        {
            var currentScrollOffset = scrollView.scrollOffset;
            
            var bounds = treeView.worldBound;
            float distanceFromTop = panelDragPosition.y - bounds.yMin;
            float distanceFromBottom = bounds.yMax - panelDragPosition.y;

            float t = 0;
            if (distanceFromTop < scrollZoneSize && distanceFromTop > 0)
            {
                t = 1f - (distanceFromTop / scrollZoneSize); // 0 at edge of zone, 1 at top
            }
            else if (distanceFromBottom < scrollZoneSize && distanceFromBottom > 0)
            {
                t = 1f - (distanceFromBottom / scrollZoneSize); // 0 at edge of zone, 1 at bottom
            }

            t = 1;
            var scrollDelta = new Vector2(0, deltaY * t * Time.deltaTime);
            scrollView.scrollOffset += scrollDelta;
            var realChange = scrollView.scrollOffset - currentScrollOffset;
            // panelDragPosition += realChange;
            
            return Mathf.Abs(realChange.y) > 0.01f;
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
            hoveredItem.EnableInClassList(toRootTargetUSSClassName, currentDropMode == DropMode.ToRoot);
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