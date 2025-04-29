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
    //make the updateselection and updateswatchfromstylechange generic to prevent coupling with cartesiantilelayergameobject
    public class LayerColorPropertySection : PropertySectionWithLayerGameObject
    {  
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject colorSwatchPrefab;
        [SerializeField] private RectTransform layerContent;

        private LayerGameObject layer;
        private List<ColorSwatch> selectedSwatches = new List<ColorSwatch>();
        private List<int> selectedIndices = new List<int>();
        private int currentButtonIndex = -1;
        private ColorSwatch[] items;

        private ColorPickerPropertySection colorPicker;

        public override LayerGameObject LayerGameObject
        {
            get => layer;
            set
            {
                layer = value;
                layer.OnStylingApplied.AddListener(UpdateSwatchFromStyleChange);
                Initialize();
            }
        }

        private void Initialize()
        {
            LoadLayerFeatures();
            StartCoroutine(WaitForEndFrame(()=>
            {
                //workaround to have a minimum height for the content loaded (because of scrollrects)
                LayoutElement layout = GetComponent<LayoutElement>();
                layout.minHeight = content.rect.height;

                //we need to wait a frame for this so all propertysections will be available after instantiation
                colorPicker = FindAnyObjectByType<ColorPickerPropertySection>();
            })); 
        }

        private IEnumerator WaitForEndFrame(Action callBack)
        {
            yield return new WaitForEndOfFrame(); 
            callBack.Invoke();
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
                string layerName = layerFeatures[i].GetAttribute(Constants.MaterialNameIdentifier).ToString(); //todo make this automatic
                swatch.SetLayerName(layerName);
                swatch.SetInputText(layerName);
                int cachedIndex = i;

                //because all ui elements will be destroyed on close an anonymous listener is fine here              
                swatch.onClickUp.AddListener(pointer => OnClickedSwatchUp(pointer, cachedIndex));
                swatch.onClickDown.AddListener(pointer => OnClickedSwatch(pointer, cachedIndex));

                //update the swatch color to match the material color
                Material mat = layerFeatures[i].Geometry as Material;               
                swatch.SetColor(mat.color);
                items[cachedIndex] = swatch; 
            }
        }

        private void OnClickedSwatch(PointerEventData eventData, int buttonIndex)
        {
            int lastButtonIndex = currentButtonIndex;
            currentButtonIndex = buttonIndex;
            ProcessLayerSelection();
            SelectSwatch(buttonIndex, !items[buttonIndex].IsSelected);
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
            if(select)
                colorPicker.SetColorPickerColor(items[buttonIndex].Color);

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

            //todo make this generic
            CartesianTileLayerGameObject cartesianTileLayerGameObject = layer as CartesianTileLayerGameObject;
            cartesianTileLayerGameObject.Applicator.SetIndices(selectedIndices);
        }

        private void UpdateSwatchFromStyleChange()
        {
            //todo make this generic
            CartesianTileLayerGameObject cartesianTileLayerGameObject = layer as CartesianTileLayerGameObject;
            foreach (int i in cartesianTileLayerGameObject.Applicator.MaterialIndices)
            {
                ColorSwatch swatch = layerContent.GetChild(i).GetComponent<ColorSwatch>();
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
            StartCoroutine(WaitForEndFrame(() =>
            {
                swatch.InputField.caretPosition = swatch.InputField.text.Length;
                swatch.InputField.selectionAnchorPosition = 0;
            }));
        }
    }
}