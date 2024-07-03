using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using SLIDDES.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class LayerManager : MonoBehaviour, IPointerDownHandler
    {
        public List<LayerUI> LayerUIsVisibleInInspector { get; set; } = new List<LayerUI>();

        [SerializeField] private LayerUI LayerUIPrefab;
        [SerializeField] private List<Sprite> layerTypeSprites;

        [SerializeField] private RectTransform layerUIContainer;

        public RectTransform LayerUIContainer => layerUIContainer;

        //Drag variables
        [SerializeField] private DragGhost dragGhostPrefab;
        public DragGhost DragGhost { get; private set; }
        [SerializeField] private RectTransform dragLine;
        public RectTransform DragLine => dragLine;
        public Vector2 DragStartOffset { get; set; }
        public bool MouseIsOverButton { get; set; }

        //Context menu
        [SerializeField] private ContextMenuUI contextMenuPrefab;
        private ContextMenuUI contextMenu;

        public void ReconstructHierarchyUIs()
        {
            DestroyAllUIs();
            foreach (Transform t in LayerData.Instance.transform)
            {
                var layer = t.GetComponent<LayerNL3DBase>();
                ConstructHierarchyUIsRecursive(layer, null);
            }
        }

        void ConstructHierarchyUIsRecursive(LayerNL3DBase layer, LayerNL3DBase parent)
        {
            InstantiateLayerItem(layer, parent);
            foreach (Transform child in layer.transform)
            {
                if (child == layer.transform)
                    continue;

                ConstructHierarchyUIsRecursive(child.GetComponent<LayerNL3DBase>(), layer);
            }
        }

        private LayerUI InstantiateLayerItem(LayerNL3DBase layer, LayerNL3DBase parent)
        {
            var layerUI = Instantiate(LayerUIPrefab, LayerUIContainer);
            layerUI.Layer = layer;
            layer.UI = layerUI;
            layer.UI.SetParent(parent?.UI, layer.transform.GetSiblingIndex());
            layerUI.RegisterWithPropertiesPanel(Properties.Instance);

            return layerUI;
        }

        private void DestroyAllUIs()
        {
            foreach (Transform t in LayerUIContainer)
            {
                Destroy(t.gameObject);
            }
        }

        private void OnEnable()
        {
            ReconstructHierarchyUIs();
            LayerData.LayerAdded.AddListener(CreateNewUI);
            // LayerData.LayerDeleted.AddListener(OnLayerDeleted);
        }

        private void OnDisable()
        {
            ProjectData.RootLayer.DeselectAllLayers();
            LayerData.LayerAdded.RemoveListener(CreateNewUI);
            // LayerData.LayerDeleted.RemoveListener(OnLayerDeleted);
        }
        
        private void CreateNewUI(LayerNL3DBase layer)
        {
            var layerUI = InstantiateLayerItem(layer, layer.transform.parent.GetComponent<LayerNL3DBase>());
            layer.SelectLayer(true);
        }

        private void OnRectTransformDimensionsChange()
        {
            foreach (var layer in LayerUIsVisibleInInspector)
            {
                layer.MarkLayerUIAsDirty();
            }
        }

        public void StartDragLayer(LayerUI layerUI)
        {
            CreateGhost(layerUI);
        }

        public void EndDragLayer()
        {
            // DraggingLayer = null;
            Destroy(DragGhost.gameObject);
            dragLine.gameObject.SetActive(false);
        }

        private void CreateGhost(LayerUI ui)
        {
            DragGhost = Instantiate(dragGhostPrefab, transform);
            DragGhost.GetComponent<RectTransform>().SetLeft(layerUIContainer.offsetMin.x);
            DragGhost.Initialize(DragStartOffset, ui);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!LayerUI.AddToSelectionModifierKeyIsPressed() && !LayerUI.SequentialSelectionModifierKeyIsPressed())
                ProjectData.RootLayer.DeselectAllLayers();
        }

        public FolderLayer CreateFolderLayer()
        {
            var newLayer = new GameObject("Folder");
            var folder = newLayer.AddComponent<FolderLayer>();
            return folder;
        }

        public Sprite GetLayerTypeSprite(LayerNL3DBase layer)
        {
            switch (layer)
            {
                case ReferencedProxyLayer _:
                    var reference = ((ReferencedProxyLayer)layer).Reference;
                    return reference == null ? layerTypeSprites[0] : GetProxyLayerSprite(reference);
                case FolderLayer _:
                    return layerTypeSprites[2];
                case DatasetLayer _:
                    return layerTypeSprites[5];
                case PolygonSelectionLayer _:
                    if (((PolygonSelectionLayer)layer).ShapeType == ShapeType.Polygon)
                        return layerTypeSprites[6];
                    return layerTypeSprites[7];
                case GeoJSONLayer _:
                    return layerTypeSprites[8]; //todo: split in points
                case GeoJSONPolygonLayer:
                    return layerTypeSprites[6];
                case GeoJSONLineLayer:
                    return layerTypeSprites[7];
                case GeoJSONPointLayer:
                    return layerTypeSprites[9];
                default:
                    Debug.LogError("layer type of " + layer.Name + " is not specified");
                    return layerTypeSprites[0];
            }
        }

        private Sprite GetProxyLayerSprite(ReferencedLayer layer)
        {
            switch (layer)
            {
                case CartesianTileLayer _:
                    return layerTypeSprites[1];
                case Tile3DLayer _:
                    return layerTypeSprites[1];
                case HierarchicalObjectLayer _:
                    return layerTypeSprites[3];
                case ObjectScatterLayer _:
                    return layerTypeSprites[4];
                default:
                    Debug.LogError("layer type of " + layer.Name + " is not specified");
                    return layerTypeSprites[0];
            }
        }

        public void EnableContextMenu(bool enable, Vector2 position = default)
        {
            if (contextMenu)
                Destroy(contextMenu.gameObject); //destroy and reinstantiate to also destroy all active submenus

            if (enable)
                contextMenu = Instantiate(contextMenuPrefab, transform);

            SetContextMenuPosition(position);
        }

        void SetContextMenuPosition(Vector2 position)
        {
            var contextMenuRectTransform = contextMenu.transform as RectTransform;
            var scaledSize = contextMenuRectTransform.rect.size * contextMenuRectTransform.lossyScale;
            var clampedPositionX = Mathf.Clamp(position.x, 0, Screen.width - scaledSize.x);
            var clampedPositionY = Mathf.Clamp(position.y, scaledSize.y, Screen.height);
            contextMenu.transform.position = new Vector2(clampedPositionX, clampedPositionY);
        }

        private void Update()
        {
            if (Keyboard.current.deleteKey.wasPressedThisFrame && !EventSystem.current.currentSelectedGameObject)
            {
                DeleteSelectedLayers();
            }

            if (!contextMenu)
                return;

            var mousePos = Pointer.current.position.ReadValue();
            var contextMenuRectTransform = contextMenu.transform as RectTransform;
            var relativePoint = contextMenuRectTransform.InverseTransformPoint(mousePos);
            if (Pointer.current.press.wasPressedThisFrame && !ContextMenuUI.OverAnyContextMenuUI)
            {
                EnableContextMenu(false);
            }
        }

        public void GroupSelectedLayers()
        {
            var newGroup = CreateFolderLayer();
            var referenceLayer = ProjectData.RootLayer.SelectedLayers.Last();
            newGroup.SetParent(referenceLayer.ParentLayer, referenceLayer.transform.GetSiblingIndex());
            SortSelectedLayers();
            foreach (var selectedLayer in ProjectData.RootLayer.SelectedLayers)
            {
                selectedLayer.SetParent(newGroup);
            }
        }

        public void SortSelectedLayersByVisibility()
        {
            ProjectData.RootLayer.SelectedLayers.Sort((layer1, layer2) => LayerUIsVisibleInInspector.IndexOf(layer1.UI).CompareTo(LayerUIsVisibleInInspector.IndexOf(layer2.UI)));
        }

        private void SortSelectedLayers()
        {
            ProjectData.RootLayer.SelectedLayers.Sort((layer1, layer2) =>
            {
                // Primary sorting by Depth
                int depthComparison = layer1.Depth.CompareTo(layer2.Depth);

                // If depths are the same, use the order as visible in the hierarchy
                return depthComparison != 0 ? depthComparison : LayerUIsVisibleInInspector.IndexOf(layer1.UI).CompareTo(LayerUIsVisibleInInspector.IndexOf(layer2.UI));
            });
        }

        public bool IsDragOnButton()
        {
            return DragGhost && MouseIsOverButton;
        }

        public void DeleteSelectedLayers()
        {
            foreach (var layer in ProjectData.RootLayer.SelectedLayers)
            {
                Destroy(layer.gameObject);
            }
        }

        public void RemoveUI(LayerUI layerUI)
        {
            if (ProjectData.RootLayer.SelectedLayers.Contains(layerUI.Layer))
                ProjectData.RootLayer.SelectedLayers.Remove(layerUI.Layer);

            if (LayerUIsVisibleInInspector.Contains(layerUI))
                LayerUIsVisibleInInspector.Remove(layerUI);
        }
    }
}