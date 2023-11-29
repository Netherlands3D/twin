using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class LayerManager : MonoBehaviour, IPointerDownHandler
    {
        public static HashSet<LayerNL3DBase> AllLayers = new HashSet<LayerNL3DBase>();
        public static List<LayerUI> LayersVisibleInInspector = new List<LayerUI>();
        public static List<LayerUI> SelectedLayers { get; set; } = new();

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

        public static void AddLayer(LayerNL3DBase newLayer)
        {
            print("adding " + newLayer.name);
            AllLayers.Add(newLayer);
        }

        public static void RemoveLayer(LayerNL3DBase layer)
        {
            AllLayers.Remove(layer);
        }

        public void RefreshLayerList()
        {
            foreach (var layer in AllLayers)
            {
                if (!layer.UI)
                {
                    var layerUI = Instantiate(LayerUIPrefab, LayerUIContainer);
                    layerUI.Layer = layer;
                    layer.UI = layerUI;

                    LayersVisibleInInspector.Add(layerUI);
                }
            }
        }

        private void OnEnable()
        {
            RefreshLayerList();
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

        public static void DeselectAllLayers()
        {
            foreach (var selectedLayer in SelectedLayers)
            {
                selectedLayer.SetHighlight(InteractionState.Default);
            }

            SelectedLayers.Clear();
        }

        public FolderLayer CreateFolderLayer()
        {
            var newLayer = new GameObject("Folder");
            var folder = newLayer.AddComponent<FolderLayer>();
            RefreshLayerList();
            return folder;
        }

        public Sprite GetLayerTypeSprite(LayerNL3DBase layer)
        {
            switch (layer)
            {
                case Tile3DLayer _:
                    // print("tile layer");
                    return layerTypeSprites[1];
                    break;
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
            if(contextMenu)
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
            if(!contextMenu)
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
            print(newGroup.name);
            var referenceLayer = SelectedLayers.Last();
            newGroup.UI.SetParent(referenceLayer.ParentUI, referenceLayer.transform.GetSiblingIndex());
            SortSelectedLayers();
            foreach (var selectedLayer in SelectedLayers)
            {
                // var skipReparent = false;
                // var checkParent = selectedLayer.ParentUI;
                // while (checkParent != null)
                // {
                //     if (SelectedLayers.Contains(checkParent))
                //     {
                //         skipReparent = true;
                //         print("parent " +checkParent.Layer.name + "of " + selectedLayer.Layer.name +" is also selected, skipping");
                //         break;
                //     }
                //     checkParent = checkParent.ParentUI;
                // }
                // if(skipReparent)
                //     continue;

                print("reparenting " + selectedLayer.Layer.name + " to " + newGroup.name);
                selectedLayer.SetParent(newGroup.UI);
            }
        }
        
        public static void SortSelectedLayersByVisibility()
        {
            SelectedLayers.Sort((layer1, layer2) => LayersVisibleInInspector.IndexOf(layer1).CompareTo(LayersVisibleInInspector.IndexOf(layer2)));
        }

        static void SortSelectedLayers()
        {
            SelectedLayers.Sort((layer1, layer2) =>
            {
                // Primary sorting by Depth
                int depthComparison = layer1.Depth.CompareTo(layer2.Depth);

                // If depths are the same, use the order as visible in the hierarchy
                return depthComparison != 0 ? depthComparison : LayersVisibleInInspector.IndexOf(layer1).CompareTo(LayersVisibleInInspector.IndexOf(layer2));
            });
        }

        public bool IsDragOnButton()
        {
            return DragGhost && MouseIsOverButton;
        }
    }
}