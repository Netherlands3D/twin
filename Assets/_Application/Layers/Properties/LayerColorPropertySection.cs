using GG.Extensions;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.UI.HierarchyInspector;
using Netherlands3D.Twin.UI.ColorPicker;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    //todo this should probably become a BinaryMeshLayerColorPropertySection
    public class LayerColorPropertySection : PropertySectionWithLayerGameObject
    {  
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject colorSwatchPrefab;
        [SerializeField] private RectTransform layerContent;

        private LayerGameObject layer;
        private List<ColorSwatch> selectedSwatches = new List<ColorSwatch>();
        private List<int> selectedIndices = new List<int>();
        private int currentButtonIndex = -1;
        private float lastClickTime;
        private ColorSwatch[] items; 

        public override LayerGameObject LayerGameObject
        {
            get => layer;
            set
            {
                layer = value;
                layer.OnStylingApplied.AddListener(UpdateSwatchFromStyleChange);
                LoadLayerFeatures();
                StartCoroutine(WaitFrame()); //workaround to have a minimum height for the content loaded (because of scrollrects)
            }
        }

        private IEnumerator WaitFrame()
        {
            yield return new WaitForEndOfFrame(); 
            LayoutElement layout = GetComponent<LayoutElement>();
            layout.minHeight = content.rect.height;
        }

        private void LoadLayerFeatures()
        {
            layerContent.ClearAllChildren();            
            List<LayerFeature> layerFeatures = layer.GetLayerFeatures().Values.ToList();
            items = new ColorSwatch[layerFeatures.Count];
            for(int i = 0; i < layerFeatures.Count; i++) 
            {
                GameObject swatchObject = Instantiate(colorSwatchPrefab, layerContent);
                ColorSwatch swatch = swatchObject.GetComponent<ColorSwatch>();
                string layerName = layerFeatures[i].Attributes.Values.FirstOrDefault().ToString();
                swatch.SetLayerName(layerName);
                swatch.SetInputText(layerName);
                int cachedIndex = i;
                //because all ui elements will be destroyed on close an anonymous listener is fine here
                //swatch.Button.onClick.AddListener(() => OnClickedSwatch(cachedIndex));
                swatch.onClickUp.AddListener(pointer => OnClickedSwatchUp(pointer, cachedIndex));
                swatch.onClickDown.AddListener(pointer => OnClickedSwatch(pointer, cachedIndex));
                Material mat = layerFeatures[i].Geometry as Material;
                swatch.SetColor(mat.color);

                items[cachedIndex] = swatch; 
            }
        }

        private void OnClickedSwatch(PointerEventData eventData, int buttonIndex)
        {
            int lastButtonIndex = currentButtonIndex;
            currentButtonIndex = buttonIndex;
            //only one extra click on a selected layer should initiate the layer name editing
            float timeSinceLastClick = Time.time - lastClickTime;
            if (lastButtonIndex == buttonIndex && 
                timeSinceLastClick > LayerUI.DoubleClickLayerThreshold && 
                eventData.pointerEnter == items[currentButtonIndex].TextField.gameObject &&
                NoModifierKeyPressed()
                )
            {
                OnSelectInputField(items[currentButtonIndex]);
            }
            else
            {                
                ProcessLayerSelection();
                SelectSwatch(buttonIndex, !items[buttonIndex].IsSelected);
            }
            
            lastClickTime = Time.time;
        }

        private bool NoModifierKeyPressed()
        {
            return !LayerUI.AddToSelectionModifierKeyIsPressed() && !LayerUI.SequentialSelectionModifierKeyIsPressed();
        }

        private void OnClickedSwatchUp(PointerEventData eventData, int buttonIndex)
        {
            //ProcessLayerSelection();
        }        

        private void SelectSwatch(int buttonIndex, bool select)
        {
            items[buttonIndex].SetSelected(select);
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            selectedIndices.Clear();
            selectedSwatches.Clear();            
            foreach (ColorSwatch swatch in items)
                if (swatch.IsSelected)
                {
                    selectedSwatches.Add(swatch);
                    selectedIndices.Add(items.IndexOf(swatch));
                }

            CartesianTileLayerGameObject cartesianTileLayerGameObject = layer as CartesianTileLayerGameObject;
            cartesianTileLayerGameObject.Applicator.SetIndices(selectedIndices);
        }

        private void UpdateSwatchFromStyleChange()
        {
            CartesianTileLayerGameObject cartesianTileLayerGameObject = layer as CartesianTileLayerGameObject;
            foreach (int i in cartesianTileLayerGameObject.Applicator.MaterialIndices)
            {
                ColorSwatch swatch = layerContent.GetChild(i).GetComponent<ColorSwatch>();
                //swatch.SetColor(cartesianTileLayerGameObject.Applicator.GetMaterialByIndex(i).color);
                swatch.SetColor(cartesianTileLayerGameObject.Applicator.GetMaterial().color);
            }
        }

        private void OnDestroy()
        {
            layer.OnStylingApplied.RemoveListener(UpdateSwatchFromStyleChange);
        }

        private void ProcessLayerSelection()
        {
            if (LayerUI.SequentialSelectionModifierKeyIsPressed() && selectedSwatches.Count > 0) //if no layers are selected, there will be no reference layer to add to
            {                
                int lastIndex = items.IndexOf(selectedSwatches[selectedSwatches.Count - 1]); //last element is always the last selected layer                               
                int targetIndex = currentButtonIndex;
                if(lastIndex > targetIndex)
                {
                    int temp = lastIndex;
                    lastIndex = targetIndex;
                    targetIndex = temp;
                }
                bool addSelection = !items[currentButtonIndex].IsSelected; 
                for (int i = lastIndex; i <= targetIndex; i++)
                    items[i].SetSelected(addSelection);
                items[currentButtonIndex].SetSelected(!addSelection);
            }
            if (NoModifierKeyPressed())
            {
                foreach (var layer in selectedSwatches)
                    layer.SetSelected(false);

            }
            UpdateSelection();
        }

        private void OnSelectInputField(ColorSwatch swatch)
        {            
            swatch.InputField.text = swatch.LayerName;
            swatch.TextField.text = swatch.LayerName;
            swatch.InputField.gameObject.SetActive(true);
            swatch.TextField.gameObject.SetActive(false);
            swatch.InputField.interactable = true;
            swatch.InputField.Select();
            swatch.InputField.ActivateInputField();
            StartCoroutine(WaitForNextFrame(() =>
            {
                swatch.InputField.caretPosition = swatch.InputField.text.Length;
                swatch.InputField.selectionAnchorPosition = 0;
            }));
        }

        private IEnumerator WaitForNextFrame(Action onNextFrame)
        {
            yield return new WaitForEndOfFrame();
            onNextFrame.Invoke();
        }
    }
}