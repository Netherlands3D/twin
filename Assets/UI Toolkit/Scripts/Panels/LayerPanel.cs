using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Projects;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using ScrollView = UnityEngine.UIElements.ScrollView;
using TreeView = Netherlands3D.UI.Components.TreeView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement, InspectorPanel]
    public partial class LayerPanel : BaseInspectorContentPanel
    {
        private const string aboveTargetUSSClassName = "layer-list-view-item--reparent-above";
        private const string reparentTargetUSSClassName = "layer-list-view-item--reparent-target";
        private const string belowTargetUSSClassName = "layer-list-view-item--reparent-below";
        private const string toRootAboveTargetUSSClassName = "layer-list-view-item--reparent-to-root-above";
        private const string toRootBelowTargetUSSClassName = "layer-list-view-item--reparent-to-root-below";
        private const string buttonHighlightUSSClassName = "button--drag-hover";

        private TreeView treeView;
        private ScrollView scrollView;
        private const float scrollSpeed = 300f; // px/s

        private LayerData rootLayer;
        private LayerDragGhost dragGhost;

        private Vector2 panelDragPosition;
        private int siblingIndex;
        private LayerListViewItem hoveredItem;

        private LayerListViewItem referenceLayerItem;
        private Button hoveredButton;

        private Button folderButton;
        private Button deleteButton;

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
            PopulateLayerPanel(ProjectData.Current.RootLayer);

            //bottom buttons
            folderButton = this.Q<Button>("FolderButton");
            folderButton.RegisterCallback<ClickEvent>(OnFolderButtonClicked);

            deleteButton = this.Q<Button>("DeleteButton");
            deleteButton.RegisterCallback<ClickEvent>(OnDeleteButtonClicked);
        }

        private void OnFolderButtonClicked(ClickEvent evt)
        {
            GroupSelectedLayers();
        }

        private void GroupSelectedLayers()
        {
            var newGroup = App.Layers.Add(new FolderPreset.Args("Folder"));
            var referenceLayer = referenceLayerItem?.layerData;
            var siblingIndex = referenceLayer == null ? -1 : referenceLayer.SiblingIndex;


            var layersToGroup = treeView.selectedItems.Cast<LayerData>().ToList(); //make a copy because creating a new folder layer will cause this new layer to be selected and therefore the other layers to be deselected.
            // SortLayersByVisualIndex(layersToGroup);
            newGroup.LayerData.SetParent(referenceLayer?.ParentLayer, siblingIndex); // only change hierarchy after caching the selection

            // SortSelectedLayers(layersToGroup); //todo: sort the selected items to maintain the order as visible in the tree view
            if (layersToGroup.Count > 1) //only group if we have multiple layers selected
            {
                foreach (LayerData selectedLayer in layersToGroup)
                {
                    selectedLayer.SetParent(newGroup.LayerData);
                }
            }

            RebuildTree();

            ExpandToItem(newGroup.LayerData);
            RestoreSelection(layersToGroup);
        }

        private void ExpandToItem(LayerData layerData)
        {
            // Walk up the hierarchy and collect all ancestors
            var ancestors = new List<LayerData>();
            var current = layerData;

            while (current is not RootLayer)
            {
                ancestors.Add(current);
                current = current.ParentLayer;
            }

            // Expand from root downward
            ancestors.Reverse();

            int parentId = -1;

            foreach (var ancestor in ancestors)
            {
                int id = parentId == -1
                    ? GetRootIdForLayerData(ancestor)
                    : GetTreeViewIdForParentIndex(parentId, ancestor);

                treeView.ExpandItem(id);
                parentId = id;
            }
        }

        private int GetRootIdForLayerData(LayerData layerData)
        {
            var parent = layerData.ParentLayer;

            if (parent is not RootLayer)
            {
                throw new NullReferenceException("LayerData is not a child of RootLayer");
            }

            var rootIds = treeView.GetRootIds();
            foreach (var id in rootIds)
            {
                if (treeView.GetItemDataForId<LayerData>(id) == layerData)
                    return id;
            }

            throw new NullReferenceException("LayerData is not a present in the tree view");
        }

        private int GetTreeViewIdForParentIndex(int parentId, LayerData layerData)
        {
            var childIds = treeView.viewController.GetChildrenIds(parentId);

            foreach (var id in childIds)
            {
                if (treeView.GetItemDataForId<LayerData>(id) == layerData)
                    return id;
            }

            throw new NullReferenceException("LayerData is not a child of parent: " + treeView.GetItemDataForId<LayerData>(parentId).Name);
        }

        // private void SortLayersByVisualIndex(List<LayerData> layers)
        // {
        //     foreach (var layerData in layers)
        //     {
        //         // Walk all indices to find which one has this LayerData as userData
        //         for (int i = 0; i < treeView.itemsSource.Count; i++)
        //         {
        //             var id = treeView.GetIdForIndex(i);
        //             var data = treeView.GetItemDataForId<LayerData>(id);
        //
        //             if (data == layerData)
        //             {
        //                 indicesToSelect.Add(i);
        //                 break;
        //             }
        //         }
        //     }
        // }

        private void RestoreSelection(List<LayerData> layersToReselect)
        {
            var indicesToSelect = new List<int>();

            foreach (var layerData in layersToReselect)
            {
                // Walk all indices to find which one has this LayerData as userData
                for (int i = 0; i < treeView.itemsSource.Count; i++)
                {
                    var id = treeView.GetIdForIndex(i);
                    var data = treeView.GetItemDataForId<LayerData>(id);

                    if (data == layerData)
                    {
                        indicesToSelect.Add(i);
                        break;
                    }
                }
            }

            treeView.SetSelection(indicesToSelect);
        }

        private void OnDeleteButtonClicked(ClickEvent evt)
        {
            DeleteSelectedLayers();
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
            layerRowElement.RegisterCallback<ClickEvent>(SetReferenceLayer);
            return layerRowElement;
        }

        private void SetReferenceLayer(ClickEvent evt)
        {
            referenceLayerItem = evt.currentTarget as LayerListViewItem;
        }

        private void BindItem(VisualElement item, int index)
        {
            if (item is not LayerListViewItem layerRowElement) return;

            var layerData = treeView.GetItemDataForIndex<LayerData>(index);
            layerRowElement.Initialize(layerData);
            layerRowElement.SelectLayerItem.AddListener(SelectItem);
            layerRowElement.DeselectLayerItem.AddListener(DeselectItem);
        }
        
        private void DeselectItem(LayerListViewItem item)
        {
            var index = treeView.GetIndexFromElement(item);
            if (treeView.selectedIndices.Contains(index))
            {
                var newSelection = treeView.selectedIndices.ToList();
                newSelection.Remove(index);
                treeView.SetSelection(newSelection);
            }
        }
        
        private void SelectItem(LayerListViewItem item)
        {
            var index = treeView.GetIndexFromElement(item);
            treeView.SetSelection(new []{ index });
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

            referenceLayerItem = source;
        }

        private void OnDraggingLayerItem(Vector2 delta, LayerListViewItem source)
        {
            panelDragPosition += delta;

            var aboveTree = panelDragPosition.y < treeView.worldBound.yMin;
            var belowTree = panelDragPosition.y > treeView.worldBound.yMax;
            var atTopEdge = aboveTree && !MoveScrollView(-scrollSpeed);
            var atBottomEdge = belowTree && !MoveScrollView(scrollSpeed);

            dragGhost.UpdatePosition(panelDragPosition);
            var hitElement = panel.Pick(panelDragPosition);

            //check for hover buttons
            if (belowTree && panelDragPosition.x > treeView.worldBound.xMin && panelDragPosition.x < treeView.worldBound.xMax) //don't account for buttons in the tree
            {
                var hitButton = hitElement as Button ?? hitElement?.GetFirstAncestorOfType<Button>();
                if(hitButton != null)
                {
                    SetHoveredButton(hitButton);
                    return;
                }
            }

            SetHoveredButton(null);
            if (atTopEdge || atBottomEdge)
            {
                currentDropMode = DropMode.ToRoot;
                if(atTopEdge)
                {
                    SetHoveredItem(treeView.Query<LayerListViewItem>().First()); //ensure we get the first item, using GetClosestItem gives jittering issues for some reason
                    siblingIndex = 0;
                }
                else
                {
                    SetHoveredItem(GetClosestItem(panelDragPosition.y));
                    siblingIndex = -1;
                }
    
                if (atTopEdge)
                    SetTargetItemUssClasses(hoveredItem, toRootAboveTargetUSSClassName);
                else
                    SetTargetItemUssClasses(hoveredItem, toRootBelowTargetUSSClassName);
                Debug.Log("toroot "+hoveredItem.layerData.Name);

                return;
            }

            // var hitElement = panel.Pick(panelDragPosition);
            var targetItem = hitElement as LayerListViewItem
                             ?? hitElement?.GetFirstAncestorOfType<LayerListViewItem>()
                             ?? GetClosestItem(panelDragPosition.y);

            SetHoveredItem(targetItem);
            Debug.Log("not anove root: " + hoveredItem.layerData.Name);
            var layer = hoveredItem.userData as LayerData;
            siblingIndex = layer.ParentLayer.ChildrenLayers.IndexOf(layer);
        }

        private void SetHoveredButton(Button hitButton)
        {
            if (hoveredButton == hitButton) return;

            if (hoveredButton != null)
                hoveredButton.EnableInClassList(buttonHighlightUSSClassName, false);

            hoveredButton = hitButton;

            if (hoveredButton != null)
                hoveredButton.EnableInClassList(buttonHighlightUSSClassName, true);
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
                    SetTargetItemUssClasses(hoveredItem, null);
                }

                hoveredItem = targetItem;
            }

            var worldTop = hoveredItem.LocalToWorld(Vector2.zero);
            float localY = panelDragPosition.y - worldTop.y;
            float normalizedY = localY / hoveredItem.layout.height;

            string newClassName = null;
            if (normalizedY < 0.25f)
            {
                currentDropMode = DropMode.Above;
                newClassName = aboveTargetUSSClassName;
            }
            else if (normalizedY > 0.75f)
            {
                currentDropMode = DropMode.Below;
                newClassName = belowTargetUSSClassName;
            }
            else
            {
                currentDropMode = DropMode.Into;
                newClassName = reparentTargetUSSClassName;
            }

            SetTargetItemUssClasses(hoveredItem, newClassName);
        }

        private void OnDraggingLayerItemEnded(Vector2 endPosition, LayerListViewItem source)
        {
            if (hoveredButton != null)
            {
                if (hoveredButton == deleteButton)
                    DeleteSelectedLayers();
                else if (hoveredButton == folderButton)
                    GroupSelectedLayers();
            }
            else if (hoveredItem != null)
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

            CleanupDrag();
        }

        private void CleanupDrag()
        {
            if (hoveredItem != null)
                SetTargetItemUssClasses(hoveredItem, null);

            if (hoveredButton != null)
                hoveredButton.EnableInClassList(buttonHighlightUSSClassName, false);

            hoveredButton = null;
            hoveredItem = null;

            dragGhost.RemoveFromHierarchy();
            dragGhost = null;
        }

        private void SetTargetItemUssClasses(LayerListViewItem targetItem, string newClassName)
        {
            targetItem.ItemRoot.EnableInClassList(aboveTargetUSSClassName, false);
            targetItem.ItemRoot.EnableInClassList(reparentTargetUSSClassName, false);
            targetItem.ItemRoot.EnableInClassList(belowTargetUSSClassName, false);
            targetItem.ItemRoot.EnableInClassList(toRootAboveTargetUSSClassName, false);
            targetItem.ItemRoot.EnableInClassList(toRootBelowTargetUSSClassName, false);

            if (newClassName != null)
                targetItem.ItemRoot.EnableInClassList(newClassName, true);
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