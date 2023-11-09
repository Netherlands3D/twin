using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class LayerManager : MonoBehaviour, IPointerDownHandler
    {
        public static HashSet<LayerNL3DBase> AllLayers = new HashSet<LayerNL3DBase>();
        public static List<LayerUI> LayersVisibleInInspector = new List<LayerUI>();
        public static List<LayerUI> SelectedLayers { get; set; } = new();

        [SerializeField] private LayerUI LayerUIPrefab;
        [SerializeField] private List<Sprite> layerTypeSprites;

        //Drag variables
        [SerializeField] private DragGhost dragGhostPrefab;
        public DragGhost DragGhost { get; private set; }
        [SerializeField] private RectTransform dragLine;
        public RectTransform DragLine => dragLine;
        public Vector2 DragStartOffset { get; set; }

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
                    var layerUI = Instantiate(LayerUIPrefab, transform);
                    layerUI.Layer = layer;
                    layer.UI = layerUI;

                    LayersVisibleInInspector.Add(layerUI);
                }
            }
        }

        private void OnEnable()
        {
            // CreateLayerUIsForAllLayers();
            RefreshLayerList();
        }

        // private void OnDisable()
        // {
        //     foreach (Transform t in transform)
        //     {
        //         Destroy(t.gameObject);
        //     }
        // }

        // private void CreateLayerUIsForAllLayers()
        // {
        //     print("generating Layer UIs");
        //     foreach (var layer in AllLayers)
        //     {
        //         var layerUI = Instantiate(LayerUIPrefab, transform);
        //         layerUI.Layer = layer;
        //         layer.UI = layerUI;
        //
        //         LayersVisibleInInspector.Add(layerUI);
        //     }
        //
        //     // LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform); //not sure why it is needed to manually force a canvas update
        //     // Canvas.ForceUpdateCanvases(); //not sure why it is needed to manually force a canvas update
        // }

        private void OnRectTransformDimensionsChange()
        {
            foreach (var layer in LayersVisibleInInspector)
            {
                layer.UpdateLayerUI();
            }
        }

        public void StartDragLayer(LayerUI layerUI)
        {
            // DraggingLayer = layerUI;
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
            DragGhost = Instantiate(dragGhostPrefab, transform.parent);
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

        //called by button in ui
        public void AddFolderLayer()
        {
            var newLayer = new GameObject();
            newLayer.AddComponent<FolderLayer>();
            RefreshLayerList();
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
    }
}