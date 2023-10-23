using System;
using System.Linq;
using Netherlands3D.TileSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.UI.Inpector
{
    public class LayersUI : MonoBehaviour
    {
        [SerializeField] private Tool layerTool;
        [SerializeField] private string accordionId = "unity-content";
        [SerializeField] private VisualTreeAsset layerAsset;
        [SerializeField] private TileSystem.TileHandler tileHandler;

        private VisualElement layerInspector;
        private void OnEnable()
        {
            layerTool.onActivate.AddListener(RefreshLayers);
        }

        private void OnDisable()
        {
            layerTool.onActivate.RemoveListener(RefreshLayers); 
        }

        private void RefreshLayers()
        {
            layerInspector = layerTool.InspectorInstance;       
            foreach (var layer in tileHandler.layers)
            {
                AddLayerUI(layer);
            }
        }

        public void AddLayerUI(Layer layer)
        {
            var layerUI = layerAsset.Instantiate().Q<Foldout>();
            layerUI.name = layer.name;
            layerUI.text = layer.name;
            var layerFoldout = layerTool.InspectorInstance.Q<Foldout>();
            ReparentLayerUI(layerFoldout, layerUI);
        }

        public void ReparentLayerUI(Foldout parent, Foldout child)
        {
            parent.contentContainer.Add(child);
            var layerFoldout = layerTool.InspectorInstance.Q<Foldout>();
            layerFoldout.Q<Foldout>("Terrain");
            print(layerFoldout.text);
        }
    }
}