using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class LayerManager : MonoBehaviour
    {
        public static HashSet<LayerNL3DBase> AllLayers = new HashSet<LayerNL3DBase>();

        public static List<LayerUI> LayersVisibleInInspector = new List<LayerUI>();

        // public static List<LayerNL3DBase> AllLayers = new List<LayerNL3DBase>();
        public static LayerUI DraggingLayer { get; set; }
        // public static LayerUI OverLayer { get; set; }

        [SerializeField] private LayerUI LayerUIPrefab;
        [SerializeField] private DragGhost dragGhostPrefab;

        [SerializeField] private DragGhost dragGhost;
        [SerializeField] private RectTransform dragLine;
        public RectTransform DragLine => dragLine;
        
        public Vector2 DragStartOffset { get; set; }

        private void OnEnable()
        {
            print("generating Layer UIs");
            foreach (var layer in AllLayers)
            {
                var layerUI = Instantiate(LayerUIPrefab, transform);
                layerUI.Layer = layer;
                layer.UI = layerUI;

                layerUI.UpdateLayerUI();

                LayersVisibleInInspector.Add(layerUI);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform); //not sure why it is needed to manually force a canvas update
            // Canvas.ForceUpdateCanvases(); //not sure why it is needed to manually force a canvas update
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
            DraggingLayer = layerUI;
            CreateGhost();
        }

        public void EndDraglayer()
        {
            DraggingLayer = null;
            Destroy(dragGhost.gameObject);
            dragLine.gameObject.SetActive(false);
        }

        private void CreateGhost()
        {
            dragGhost = Instantiate(dragGhostPrefab, transform.parent);
            dragGhost.Initialize(DragStartOffset);
        }

        // public void OnLayerEnter(LayerUI hoveringLayer)
        // {
        //     OverLayer = hoveringLayer; //todo: make this still work when not dragging directly over layer
        // }
    }
}