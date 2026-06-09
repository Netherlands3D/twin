using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Projects;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using Button = UnityEngine.UIElements.Button;
using ScrollView = UnityEngine.UIElements.ScrollView;
using TreeView = Netherlands3D.UI.Components.TreeView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement, InspectorPanel]
    public partial class LayerPanel : BaseInspectorContentPanel
    {
        private const string reparentTargetUSSClassName = "layer-list-view-item--reparent-target";
        private const string buttonHighlightUSSClassName = "button--drag-hover";

        private TreeView treeView;
        private ScrollView scrollView;
        private const float scrollSpeed = 300f; // px/s

        private LayerData rootLayer;
        private LayerDragGhost dragGhost;

        private Vector2 panelDragPosition;
        private int siblingIndex;
        private LayerTreeViewItem hoveredItem;

        private LayerTreeViewItem referenceLayerItem;
        private Button hoveredButton;

        private Button folderButton;
        private Button deleteButton;

        public enum DropMode
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
            treeView.focusable = true;

            treeView.makeItem = MakeItem;
            treeView.bindItem = BindItem;

            treeView.selectionChanged += OnSelectionChanged;
            treeView.RegisterCallback<BlurEvent>(OnBlur);

            scrollView = treeView.Q<ScrollView>();

            RegisterCallback<KeyDownEvent>(OnKeyDown);
            PopulateLayerPanel(ProjectData.Current.RootLayer);

            //bottom buttons
            folderButton = this.Q<Button>("FolderButton");
            folderButton.RegisterCallback<ClickEvent>(OnFolderButtonClicked);

            deleteButton = this.Q<Button>("DeleteButton");
            deleteButton.RegisterCallback<ClickEvent>(OnDeleteButtonClicked);
        }

        private void OnBlur(BlurEvent evt)
        {
            var pos = Pointer.current.position.ReadValue();
            var panelPos = RuntimePanelUtils.ScreenToPanel(
                treeView.panel,
                new Vector2(pos.x, Screen.height - pos.y)
            );
            if (!treeView.worldBound.Contains(panelPos))
            {
                treeView.ClearSelection();
                referenceLayerItem = null;
            }
        }

        private void OnSelectionChanged(IEnumerable<object> selectedObjects)
        {
            var layerDatas = selectedObjects.Cast<LayerData>().ToList();

            ProjectData.Current.RootLayer.DeselectAllLayers();

            foreach (LayerData data in layerDatas)
            {
                if (!data.IsSelected)
                    data.SelectLayer();
            }
        }

        private void OnFolderButtonClicked(ClickEvent evt)
        {
            CreateFolderAndGroupLayers(treeView.selectedIndices.Count() > 1); //only group if we have multiple layers selected
        }

        private void CreateFolderAndGroupLayers(bool group)
        {
            var layersToGroup = treeView.selectedItems.Cast<LayerData>().OrderBy(GetTreeViewIndexForLayerData).ToList(); //make a copy because creating a new folder layer will cause this new layer to be selected and therefore the other layers to be deselected.

            var newGroup = App.Layers.Add(new FolderPreset.Args("Folder"));
            var referenceLayer = referenceLayerItem?.layerData;
            var siblingIndex = referenceLayer == null ? -1 : referenceLayer.SiblingIndex;

            newGroup.LayerData.SetParent(referenceLayer?.ParentLayer, siblingIndex); // only change hierarchy after caching the selection

            if (group)
            {
                foreach (LayerData selectedLayer in layersToGroup)
                {
                    selectedLayer.SetParent(newGroup.LayerData);
                }
            }

            RebuildTree();

            ExpandToItem(newGroup.LayerData);
            if (group)
                RestoreSelection(layersToGroup);
            else
                RestoreSelection(new List<LayerData>(){newGroup.LayerData});
        }

        private int GetTreeViewIndexForLayerData(LayerData layerData)
        {
            // Walk up to collect ancestors
            var ancestors = GetAncestors(layerData);

            // Walk down using existing functions to find the ID
            int parentId = -1;
            int id = -1;

            foreach (var ancestor in ancestors)
            {
                id = parentId == -1
                    ? GetRootIdForLayerData(ancestor)
                    : GetTreeViewIdForParentIndex(parentId, ancestor);

                parentId = id;
            }

            return treeView.viewController.GetIndexForId(id);
        }

        private static List<LayerData> GetAncestors(LayerData layerData)
        {
            var ancestors = new List<LayerData>();
            var current = layerData;

            while (current is not RootLayer)
            {
                ancestors.Add(current);
                current = current.ParentLayer;
            }

            ancestors.Reverse();
            return ancestors;
        }

        private void ExpandToItem(LayerData layerData)
        {
            // Walk up the hierarchy and collect all ancestors
            var ancestors = GetAncestors(layerData);

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
            var layerRowElement = new LayerTreeViewItem();
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
            referenceLayerItem = evt.currentTarget as LayerTreeViewItem;
        }

        private void BindItem(VisualElement item, int index)
        {
            if (item is not LayerTreeViewItem layerRowElement) return;

            var layerData = treeView.GetItemDataForIndex<LayerData>(index);
            layerRowElement.Initialize(layerData);
            layerRowElement.SelectLayerItem.AddListener(SelectItemWithoutNotify);
            layerRowElement.DeselectLayerItem.AddListener(DeselectWithoutNotify);

            if (layerData.IsSelected)
            {
                SelectItemWithoutNotify(layerRowElement);
            }
        }

        private void DeselectWithoutNotify(LayerTreeViewItem item)
        {
            var index = treeView.GetIndexFromElement(item);
            if (treeView.selectedIndices.Contains(index))
            {
                var newSelection = treeView.selectedIndices.ToList();
                newSelection.Remove(index);
                treeView.SetSelectionWithoutNotify(newSelection);
            }
        }

        private void SelectItemWithoutNotify(LayerTreeViewItem item)
        {
            var index = treeView.GetIndexFromElement(item);
            var newIndices = treeView.selectedIndices.ToList();
            newIndices.Add(index);
            treeView.SetSelectionWithoutNotify(newIndices);
        }

        public override string Title => "Lagen";

        private void OnDraggingLayerItemStarted(Vector2 startPosition, LayerTreeViewItem source)
        {
            if (dragGhost != null)
                dragGhost.RemoveFromHierarchy();

            panelDragPosition = startPosition;

            dragGhost = new LayerDragGhost();
            Add(dragGhost);
            dragGhost.Initialize(panelDragPosition, source);

            referenceLayerItem = source;
        }

        private void OnDraggingLayerItem(Vector2 delta, LayerTreeViewItem source)
        {
            panelDragPosition += delta;

            var aboveTree = panelDragPosition.y < scrollView.contentContainer.worldBound.yMin;
            var belowTree = panelDragPosition.y > scrollView.contentContainer.worldBound.yMax;
            var atTopEdge = aboveTree && !MoveScrollView(-scrollSpeed);
            var atBottomEdge = belowTree && !MoveScrollView(scrollSpeed);

            dragGhost.UpdatePosition(panelDragPosition);
            var hitElement = panel.Pick(panelDragPosition);

            //check for hover buttons
            if (belowTree && panelDragPosition.x > worldBound.xMin && panelDragPosition.x < worldBound.xMax) //don't account for buttons in the tree
            {
                var hitButton = hitElement as Button ?? hitElement?.GetFirstAncestorOfType<Button>();
                if (hitButton != null)
                {
                    SetHoveredButton(hitButton);
                    return;
                }
            }

            SetHoveredButton(null);
            if (atTopEdge || atBottomEdge)
            {
                if (atTopEdge)
                {
                    SetHoveredItem(treeView.Query<LayerTreeViewItem>().First()); //ensure we get the first item, using GetClosestItem gives jittering issues for some reason
                    siblingIndex = 0;
                }
                else
                {
                    SetHoveredItem(GetClosestItem(panelDragPosition.y));
                    siblingIndex = -1;
                }

                currentDropMode = DropMode.ToRoot; //override the drop mode set by SetHoveredItem
                dragGhost.UpdateLine(hoveredItem, currentDropMode);
                return;
            }

            var targetItem = hitElement as LayerTreeViewItem
                             ?? hitElement?.GetFirstAncestorOfType<LayerTreeViewItem>()
                             ?? GetClosestItem(panelDragPosition.y);

            SetHoveredItem(targetItem);
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

        private LayerTreeViewItem GetClosestItem(float worldY)
        {
            var viewportBounds = scrollView.contentViewport.worldBound;
            LayerTreeViewItem closest = null;
            float closestDistance = float.MaxValue;
            treeView.Query<LayerTreeViewItem>().ForEach(item =>
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

        private void SetHoveredItem(LayerTreeViewItem targetItem)
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
                currentDropMode = DropMode.Above;
            else if (normalizedY > 0.75f)
                currentDropMode = DropMode.Below;
            else
                currentDropMode = DropMode.Into;

            dragGhost.UpdateLine(targetItem, currentDropMode);
            SetTargetItemUssClasses(hoveredItem, newClassName);
        }

        private void OnDraggingLayerItemEnded(Vector2 endPosition, LayerTreeViewItem source)
        {
            if (hoveredButton != null)
            {
                if (hoveredButton == deleteButton)
                    DeleteSelectedLayers();
                else if (hoveredButton == folderButton)
                    CreateFolderAndGroupLayers(true); //always group when dragging on the button
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
            if (hoveredButton != null)
                hoveredButton.EnableInClassList(buttonHighlightUSSClassName, false);

            hoveredButton = null;
            hoveredItem = null;

            dragGhost.RemoveFromHierarchy();
            dragGhost = null;
        }

        private void SetTargetItemUssClasses(LayerTreeViewItem targetItem, string newClassName)
        {
            targetItem.ItemRoot.EnableInClassList(reparentTargetUSSClassName, false);

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