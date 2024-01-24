using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class LayerManager : MonoBehaviour, IPointerDownHandler
    {
        public List<LayerUI> LayersVisibleInInspector { get; set; } = new List<LayerUI>();
        public List<LayerUI> SelectedLayers { get; set; } = new();
        
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
            var layerUI = Instantiate(LayerUIPrefab, LayerUIContainer);
            layerUI.Layer = layer;
            layer.UI = layerUI;
            layer.UI.SetParent(parent?.UI, layer.transform.GetSiblingIndex());
            // layer.SetParent(parent);

            LayersVisibleInInspector.Add(layerUI);
            foreach (Transform child in layer.transform)
            {
                if (child == layer.transform)
                    continue;

                ConstructHierarchyUIsRecursive(child.GetComponent<LayerNL3DBase>(), layer);
            }
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
            LayerData.LayerDeleted.AddListener(OnLayerDeleted);
        }

        private void OnDisable()
        {
            DeselectAllLayers();
            LayerData.LayerAdded.RemoveListener(CreateNewUI);
            LayerData.LayerDeleted.RemoveListener(OnLayerDeleted);
        }

        private void CreateNewUI(LayerNL3DBase layer)
        {
            var layerUI = Instantiate(LayerUIPrefab, LayerUIContainer);
            layerUI.Layer = layer;
            layer.UI = layerUI;
            layer.UI.SetParent(layer.transform.parent.GetComponent<LayerNL3DBase>()?.UI, layer.transform.GetSiblingIndex());

            LayersVisibleInInspector.Add(layerUI);
            layerUI.Select(true);
        }

        private void OnLayerDeleted(LayerNL3DBase layer)
        {
            if (SelectedLayers.Contains(layer.UI))
            {
                layer.OnDeselect();
                SelectedLayers.Remove(layer.UI);
            }

            Destroy(layer.UI.gameObject);
        }

        private void OnRectTransformDimensionsChange()
        {
            foreach (var layer in LayersVisibleInInspector)
            {
                layer.UpdateLayerUI();
            }
        }

        public void StartDragLayer(LayerUI layerUI)
        {
            CreateGhost();
        }

        public void EndDragLayer()
        {
            // DraggingLayer = null;
            Destroy(DragGhost.gameObject);
            dragLine.gameObject.SetActive(false);
        }

        private void CreateGhost()
        {
            DragGhost = Instantiate(dragGhostPrefab, transform);
            DragGhost.Initialize(DragStartOffset);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!LayerUI.AddToSelectionModifierKeyIsPressed() && !LayerUI.SequentialSelectionModifierKeyIsPressed())
                DeselectAllLayers();
        }

        public void DeselectAllLayers()
        {
            foreach (var selectedLayer in SelectedLayers)
            {
                selectedLayer.SetHighlight(InteractionState.Default);
                selectedLayer.Layer.OnDeselect();
            }

            SelectedLayers.Clear();
        }

        public FolderLayer CreateFolderLayer()
        {
            var newLayer = new GameObject("Folder");
            var folder = newLayer.AddComponent<FolderLayer>();
            // AddMissingLayersToInspector();
            return folder;
        }

        public Sprite GetLayerTypeSprite(LayerNL3DBase layer)
        {
            switch (layer)
            {
                case ReferencedProxyLayer _:
                    // print("tile layer");
                    var referenceSprite = ((ReferencedProxyLayer)layer).Reference.LayerTypeSprite;
                    return referenceSprite == null ? layerTypeSprites[0] : referenceSprite;
                case FolderLayer _:
                    // print("folder layer");
                    return layerTypeSprites[2];
                case ObjectLayer _:
                    // print("object layer");
                    return layerTypeSprites[3];
                case ObjectScatterLayer _:
                    // print("object scatter layer");
                    return layerTypeSprites[4];
                case DatasetLayer _:
                    // print("dataset layer");
                    return layerTypeSprites[5];
                case PolygonSelectionLayer _:
                    // print("polygon selection layer");
                    return layerTypeSprites[6];
                default:
                    Debug.LogError("layer type of " + layer.name + " is not specified");
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
            if (Keyboard.current.deleteKey.wasPressedThisFrame)
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
            var referenceLayerUI = SelectedLayers.Last();
            newGroup.SetParent(referenceLayerUI.Layer.transform.parent.GetComponent<LayerNL3DBase>(), referenceLayerUI.transform.GetSiblingIndex());
            SortSelectedLayers();
            foreach (var selectedLayer in SelectedLayers)
            {
                selectedLayer.Layer.SetParent(newGroup);
            }
        }

        public void SortSelectedLayersByVisibility()
        {
            SelectedLayers.Sort((layer1, layer2) => LayersVisibleInInspector.IndexOf(layer1).CompareTo(LayersVisibleInInspector.IndexOf(layer2)));
        }

        private void SortSelectedLayers()
        {
            SelectedLayers.Sort((ui1, ui2) =>
            {
                // Primary sorting by Depth
                int depthComparison = ui1.Layer.Depth.CompareTo(ui2.Layer.Depth);

                // If depths are the same, use the order as visible in the hierarchy
                return depthComparison != 0 ? depthComparison : LayersVisibleInInspector.IndexOf(ui1).CompareTo(LayersVisibleInInspector.IndexOf(ui2));
            });
        }

        public bool IsDragOnButton()
        {
            return DragGhost && MouseIsOverButton;
        }
        
        public void DeleteSelectedLayers()
        {
            foreach (var layerUI in SelectedLayers)
            {
                Destroy(layerUI.Layer.gameObject);
            }
        }

        public void RemoveUI(LayerUI layerUI)
        {
            if (SelectedLayers.Contains(layerUI))
                SelectedLayers.Remove(layerUI);

            if (LayersVisibleInInspector.Contains(layerUI))
                LayersVisibleInInspector.Remove(layerUI);
        }
    }
}