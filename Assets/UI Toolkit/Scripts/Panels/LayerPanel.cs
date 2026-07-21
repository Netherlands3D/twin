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
using UnityEngine.InputSystem;
using Button = UnityEngine.UIElements.Button;
using ScrollView = UnityEngine.UIElements.ScrollView;
using TreeView = Netherlands3D.UI.Components.TreeView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement, InspectorPanel]
    public partial class LayerPanel : BaseInspectorContentPanel
    {
        private const string reparentTargetUSSClassName = "layer-tree-view-item--reparent-target";
        private const string buttonHighlightUSSClassName = "button--drag-hover";

        private TreeView treeView;
        private ScrollView scrollView;
        private const float scrollSpeed = 300f; // px/s

        private LayerData rootLayer;
        private LayerDragGhost dragGhost;
        private float dropMargin = 0.25f; //top 25% and bottom 25% are for reordering, 25%-75% is for reparenting

        private Vector2 panelDragPosition;
        private int siblingIndex;
        private LayerTreeViewItem hoveredItem;

        private LayerTreeViewItem referenceLayerItem;
        private Button hoveredButton;

        private Button folderButton;
        private Button deleteButton;
        
        private bool doRefresh;
        private bool doRebuild;
        private bool doReselect;

        public enum DropMode
        {
            Above,
            Into,
            Below,
            ToRootAbove,
            ToRootBelow
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
            treeView.unbindItem = UnbindItem;

            treeView.selectionChanged += OnSelectionChanged;

            scrollView = treeView.Q<ScrollView>();

            RegisterCallback<KeyDownEvent>(OnKeyDown);
            PopulateLayerPanel(ProjectData.Current.RootLayer);

            //bottom buttons
            folderButton = this.Q<Button>("FolderButton");
            folderButton.RegisterCallback<ClickEvent>(OnFolderButtonClicked);

            deleteButton = this.Q<Button>("DeleteButton");
            deleteButton.RegisterCallback<ClickEvent>(OnDeleteButtonClicked);

            dragGhost = this.Q<LayerDragGhost>();
            dragGhost.SetVisible(false);
            
            App.Layers.LayerAdded.AddListener(OnLayerHierarchyChanged);
            App.Layers.LayerRemoved.AddListener(OnLayerHierarchyChanged);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            
            schedule.Execute(() =>
            {
                if (doRebuild)
                {
                    RebuildTree();
                    doRebuild = false;
                }

                if (doRefresh)
                {
                    treeView.RefreshItems();
                    doRefresh = false;
                }

                if (doReselect)
                {
                    RestoreSelection();
                    doReselect = false;
                }
            }).Every(0); // 0ms = runs every frame
        }

        private void OnLayerHierarchyChanged(LayerData changedLayer)
        {
            OnRequestRebuild();
        }
        
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            App.Layers.LayerAdded.RemoveListener(OnLayerHierarchyChanged);
            App.Layers.LayerRemoved.RemoveListener(OnLayerHierarchyChanged);
        }

        public override void OnInspectorClick(InspectorPanel inspector)
        {
            var pos = Pointer.current.position.ReadValue();
            var panelPos = RuntimePanelUtils.ScreenToPanel(
                inspector.panel,
                new Vector2(pos.x, Screen.height - pos.y)
            );

            var inInspectorPanel = inspector.worldBound.Contains(panelPos);
            var inTreeViewLayerContainer = scrollView.contentContainer.worldBound.Contains(panelPos);
            var overButton = deleteButton.worldBound.Contains(panelPos) || folderButton.worldBound.Contains(panelPos);
            if (inInspectorPanel && !inTreeViewLayerContainer && !overButton)
            {
                treeView.ClearSelection();
                referenceLayerItem = null;
            }
        }

        private void OnSelectionChanged(IEnumerable<object> selectedObjects)
        {
            var layerDatas = selectedObjects.Cast<LayerData>().ToList(); //Make a copy to ensure we have a collection that is not modified due to deselecting

            ProjectData.Current.RootLayer.DeselectAllLayers();

            foreach (LayerData data in layerDatas)
            {
                if (!data.IsSelected)
                    data.SelectLayer();
            }
        }

        private void ToggleSelection(int clickedRootIndex, bool active)
        {
            var selectedTreeIndices = treeView.selectedIndices.ToList(); 
            
            //convert the treeIndex to the rootIndex. The tree index ignores collapsed items, and only counts visible treeViewItems,
            //the rootIndex is the stable index of the item in the tree (including collapsed items)
            var toggledSelectedLayer = false;
            for (var i = 0; i < selectedTreeIndices.Count; i++)
            {
                var treeIndex = selectedTreeIndices[i];
                var rootIndex = treeView.GetIdForIndex(treeIndex);
                if(rootIndex == clickedRootIndex)
                {
                    toggledSelectedLayer = true;
                    break;
                }
            }
            
            if (!toggledSelectedLayer) //we toggled a different layer than the selected layers, don't toggle the selected layers
                return;

            foreach (var index in selectedTreeIndices) //make a copy of the indices, because they might change
            {
                var layerData = treeView.GetItemDataForIndex<LayerData>(index);
                layerData.ActiveSelf = active;
            }

            doRefresh = true;
        }
        
        private void OnFolderButtonClicked(ClickEvent evt)
        {
            CreateFolderAndGroupLayers(treeView.selectedIndices.Count() > 1); //only group if we have multiple layers selected
        }

        private void CreateFolderAndGroupLayers(bool group)
        {
            var layersToGroup = treeView.selectedItems.Cast<LayerData>().ToList(); //make a copy with ToList because creating a new folder layer will cause this new layer to be selected and therefore the other layers to be deselected.
            layersToGroup.OrderBy(l => l.RootId);

            var newGroup = App.Layers.Add(new FolderPreset.Args("Folder"));
            var referenceLayer = referenceLayerItem?.LayerData;
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

            RequestSelection(group ? layersToGroup : new List<LayerData>() { newGroup.LayerData });
        }

        private void ExpandToItem(LayerData layerData)
        {
            // Walk up the hierarchy and collect all ancestors
            var ancestors = layerData.GetAncestors();

            foreach (var ancestor in ancestors)
            {
                treeView.ExpandItem(ancestor.RootId);
            }
        }

        private List<LayerData> selectionToRestore = new List<LayerData>();
        private void RequestSelection(List<LayerData> selection)
        {
            doReselect = true;
            selectionToRestore = selection;
        }
        
        private void RestoreSelection()
        {
            var indicesToSelect = new List<int>();

            foreach (var layer in selectionToRestore)
            {
                indicesToSelect.Add(layer.RootId);
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

        private void OnRequestRefresh()
        {
            doRefresh = true;
        }

        private void OnRequestRebuild()
        {
            doRebuild = true;
        }
        
        public void RebuildTree()
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
            layerRowElement.RequestTreeRefresh.AddListener(OnRequestRefresh);
            layerRowElement.RequestTreeRebuild.AddListener(OnRequestRebuild);
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
            layerRowElement.VisibilityToggleChanged.AddListener(ToggleSelection);

            if (layerData.IsSelected)
            {
                SelectItemWithoutNotify(layerRowElement);
            }
        }

        private void UnbindItem(VisualElement item, int index)
        {
            if (item is not LayerTreeViewItem layerRowElement) return;

            layerRowElement.RemoveLayerDataListeners(layerRowElement.LayerData);
            layerRowElement.SelectLayerItem.RemoveListener(SelectItemWithoutNotify);
            layerRowElement.DeselectLayerItem.RemoveListener(DeselectWithoutNotify);
            layerRowElement.VisibilityToggleChanged.RemoveListener(ToggleSelection);
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
            panelDragPosition = startPosition;

            dragGhost.SetVisible(true);
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
                //we are dragging over a button, don't do any reorder logic
                var hitButton = hitElement as Button ?? hitElement?.GetFirstAncestorOfType<Button>();
                if (hitButton != null)
                {
                    SetHoveredButton(hitButton);
                    return;
                }
            }

            //we are not/no longer over a button, reset the hoverButton
            SetHoveredButton(null);
            
            //we are dragging outside of the tree, we need to reorder to the root layer
            if (atTopEdge || atBottomEdge)
            {
                if (atTopEdge) // above the first layer item, reorder to the root with the top item as the reference layer
                {
                    SetHoveredItem(treeView.Query<LayerTreeViewItem>().First()); //ensure we get the first item, using GetClosestItem gives jittering issues for some reason
                    currentDropMode = DropMode.ToRootAbove; //override the drop mode set by SetHoveredItem
                    siblingIndex = 0;
                }
                else // below the last layer item, reorder to the root with the bottom item as the reference layer
                {
                    SetHoveredItem(GetClosestItem(panelDragPosition.y));
                    currentDropMode = DropMode.ToRootBelow; //override the drop mode set by SetHoveredItem
                    siblingIndex = -1; // setting the sibling index to -1 makes it just add the layer to the end of the RootLayer's children
                }

                //reset the hover state and ghost line for the ToRoot cases
                hoveredItem.ItemRoot.EnableInClassList(reparentTargetUSSClassName, false);
                dragGhost.UpdateLine(hoveredItem, currentDropMode);
                return;
            }

            //regular reorder within the tree structure
            var targetItem = hitElement as LayerTreeViewItem //we already have a LayerTreeViewItem
                             ?? GetClosestItem(panelDragPosition.y); //get the closest LayerTreeViewItem, for example when we are in a margin in between 2 items, or horizontally outside of the item

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
                    hoveredItem.ItemRoot.EnableInClassList(reparentTargetUSSClassName, false);
                }

                hoveredItem = targetItem;
            }

            var worldTop = hoveredItem.LocalToWorld(Vector2.zero);
            float localY = panelDragPosition.y - worldTop.y;
            float normalizedY = localY / hoveredItem.layout.height;

            if (normalizedY < dropMargin)
            {
                currentDropMode = DropMode.Above;
                hoveredItem.ItemRoot.EnableInClassList(reparentTargetUSSClassName, false);
            }
            else if (normalizedY > (1 - dropMargin))
            {
                currentDropMode = DropMode.Below;
                hoveredItem.ItemRoot.EnableInClassList(reparentTargetUSSClassName, false);
            }
            else
            {
                currentDropMode = DropMode.Into;
                hoveredItem.ItemRoot.EnableInClassList(reparentTargetUSSClassName, true);
            }

            dragGhost.UpdateLine(targetItem, currentDropMode);
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
                    case DropMode.ToRootAbove:
                        ReparentToLayer(selectedLayers, rootLayer, siblingIndex);
                        break;
                    case DropMode.ToRootBelow:
                        ReparentToLayer(selectedLayers, rootLayer, siblingIndex);
                        break;
                }
            }

            CleanupDrag();
        }

        private void CleanupDrag()
        {
            if(hoveredItem != null)
                hoveredItem.ItemRoot.EnableInClassList(reparentTargetUSSClassName, false);
            
            if (hoveredButton != null)
                hoveredButton.EnableInClassList(buttonHighlightUSSClassName, false);

            hoveredButton = null;
            hoveredItem = null;

            dragGhost.SetVisible(false);
        }

        private void ReparentToLayer(List<object> selectedLayers, LayerData newParent, int newSiblingIndex)
        {
            var selection = selectedLayers.Cast<LayerData>().ToList(); //Make a copy to ensure we have a collection that is not modified due to the reparenting
            foreach (LayerData selectedLayer in selection)
            {
                selectedLayer.SetParent(newParent, newSiblingIndex);
            }

            RequestSelection(selection);
        }
    }
}