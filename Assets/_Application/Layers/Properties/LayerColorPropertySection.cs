using GG.Extensions;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.UI.HierarchyInspector;
using Netherlands3D.Twin.UI.ColorPicker;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
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
        private LayerGameObject layer;
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject colorSwatchPrefab;
        [SerializeField] private RectTransform layerContent;

        private List<ColorSwatch> selectedSwatches = new List<ColorSwatch>();

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
            for(int i = 0; i < layerFeatures.Count; i++) 
            {
                GameObject swatchObject = Instantiate(colorSwatchPrefab, layerContent);
                ColorSwatch swatch = swatchObject.GetComponent<ColorSwatch>();
                swatch.SetInputText(layerFeatures[i].Attributes.Values.FirstOrDefault().ToString());
                int cachedIndex = i;
                //because all ui elements will be destroyed on close an anonymous listener is fine here
                swatch.Button.onClick.AddListener(() => OnClickedSwatch(cachedIndex));
                swatch.onClickUp.AddListener(() => OnClickedSwatchUp(cachedIndex));
                Material mat = layerFeatures[i].Geometry as Material;
                swatch.SetColor(mat.color);
            }
        }

        int currentButtonIndex = -1;
        private void OnClickedSwatch(int buttonIndex)
        {
         

            currentButtonIndex = buttonIndex;
            SelectSwatch(buttonIndex, !GetSwatch(buttonIndex).IsSelected);


            //ProcessLayerSelection();

           

        }

        private void OnClickedSwatchUp(int buttonIndex)
        {

        }

        private ColorSwatch GetSwatch(int buttonIndex)
        {
            return layerContent.GetChild(buttonIndex).GetComponent<ColorSwatch>();
        }

        private void SelectSwatch(int buttonIndex, bool select)
        {
            ColorSwatch swatch = GetSwatch(buttonIndex);
            swatch.SetSelected(select);
            UpdateSelection();
        }

        private List<int> selectedIndices = new List<int>();
        private void UpdateSelection()
        {
            selectedIndices.Clear();
            selectedSwatches.Clear();
            ColorSwatch[] swatches = layerContent.GetComponentsInChildren<ColorSwatch>();
            foreach (ColorSwatch swatch in swatches)
                if (swatch.IsSelected)
                {
                    selectedSwatches.Add(swatch);
                    selectedIndices.Add(swatches.IndexOf(swatch));
                }

            CartesianTileLayerGameObject cartesianTileLayerGameObject = layer as CartesianTileLayerGameObject;
            cartesianTileLayerGameObject.Applicator.SetIndices(selectedIndices);
        }

        private void ColorSelection()
        {
           
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
                // add all layers between the currently selected layer and the reference layer
                var lastIndex = selectedSwatches.Count - 1; //last element is always the last selected layer                               

                var startIndex = lastIndex > currentButtonIndex ? currentButtonIndex + 1 : lastIndex + 1;
                var endIndex = lastIndex > currentButtonIndex ? lastIndex - 1 : currentButtonIndex - 1;

                var addLayers = !selectedSwatches[currentButtonIndex].IsSelected; //add or subtract layers?

                for (int i = startIndex; i <= endIndex; i++)
                {
                    selectedSwatches[i].SetSelected(addLayers);
                }
            }

            if (!LayerUI.AddToSelectionModifierKeyIsPressed() && !LayerUI.SequentialSelectionModifierKeyIsPressed())
                foreach(var layer in selectedSwatches)
                    layer.SetSelected(false);            
        }
    }
}