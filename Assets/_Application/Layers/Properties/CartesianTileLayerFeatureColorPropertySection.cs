using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.UI.HierarchyInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class CartesianTileLayerFeatureColorPropertySection : PropertySectionWithLayerGameObject
    {  
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject colorSwatchPrefab;
        [SerializeField] private RectTransform layerContent;

        private LayerGameObject layer;
        private readonly Dictionary<LayerFeature, ColorSwatch> swatches = new();
        [SerializeField] private ColorPickerPropertySection colorPicker;
        private int currentButtonIndex = -1;
        private List<ColorSwatch> selectedItems = new();
        private ColorSwatch firstSelectedItem;

        public override LayerGameObject LayerGameObject
        {
            get => layer;
            set => Initialize(value);
        }

        private void Initialize(LayerGameObject layer)
        {
            this.layer = layer;
            CreateSwatches();

            layer.OnStylingApplied.AddListener(UpdateSwatches);

            StartCoroutine(OnPropertySectionsLoaded()); 
        }

        private void OnDestroy()
        {
            layer.OnStylingApplied.RemoveListener(UpdateSwatches);
            colorPicker.PickedColor.RemoveListener(OnPickColor);
        }

        private IEnumerator OnPropertySectionsLoaded()
        {
            yield return new WaitForEndOfFrame();

            // Reset listeners to prevent default behaviour
            colorPicker.PickedColor.RemoveAllListeners();
            colorPicker.LayerGameObject = layer;
            colorPicker.PickedColor.AddListener(OnPickColor);
            HideColorPicker();

            // workaround to have a minimum height for the content loaded (because of scrollrects)
            LayoutElement layout = GetComponent<LayoutElement>();
            layout.minHeight = content.rect.height;
        }

        private void CreateSwatches()
        {
            swatches.Clear();
            layerContent.ClearAllChildren();
            foreach (var layerFeature in layer.LayerFeatures.Values)
            {
                swatches[layerFeature] = CreateSwatch(layerFeature);
                SetSwatchColorFromFeature(layerFeature);
            }
        }

        private ColorSwatch CreateSwatch(LayerFeature layerFeature)
        {
            GameObject swatchObject = Instantiate(colorSwatchPrefab, layerContent);
            ColorSwatch swatch = swatchObject.GetComponent<ColorSwatch>();
                
            string layerName = layerFeature.GetAttribute(CartesianTileLayerStyler.MaterialNameIdentifier);
                
            swatch.SetLayerName(layerName);
            swatch.SetInputText(layerName);

            //because all ui elements will be destroyed on close an anonymous listener is fine here              
            swatch.onClickDown.AddListener(pointer => OnClickedOnSwatch(pointer, swatch));

            return swatch;
        }

        private void OnClickedOnSwatch(PointerEventData _, ColorSwatch swatch)
        {
            //select layer
            currentButtonIndex = swatches.Values.ToList().IndexOf(swatch);
            ProcessLayerSelection();
            UpdateSelection();

            //if (swatch.IsSelected)
            //{
            //    DeselectAllSwatches();
            //    DeselectSwatch(swatch);
            //    return;
            //}

            //DeselectAllSwatches();
            //SelectSwatch(swatch);
        }

        //private void SelectSwatch(ColorSwatch swatch)
        //{
        //    ShowColorPicker();
        //    colorPicker.PickColorWithoutNotify(swatch.Color);

        //    swatch.SetSelected(true);


        //}

        //private void DeselectAllSwatches()
        //{
        //    foreach (var (_, swatch) in swatches)
        //    {
        //        if (swatch.IsSelected) continue;

        //        DeselectSwatch(swatch);
        //    }
        //}

        //private void DeselectSwatch(ColorSwatch swatch)
        //{
        //    swatch.SetSelected(false);

        //    HideColorPicker();
        //}

        private void OnPickColor(Color color)
        {
            foreach ((LayerFeature layerFeature, ColorSwatch swatch) in swatches)
            {
                if (!swatch.IsSelected) continue;
                
                swatch.SetColor(color);
                SetColorizationStylingRule(layerFeature, color);
            }
        }

        private void SetColorizationStylingRule(LayerFeature layerFeature, Color color)
        {
            (layer.Styler as CartesianTileLayerStyler).SetColor(layerFeature, color);
        }

        private void UpdateSwatches()
        {
            foreach (var (layerFeature, _) in swatches)
            {
                SetSwatchColorFromFeature(layerFeature);
            }
        }

        private void SetSwatchColorFromFeature(LayerFeature layerFeature)
        {
            // if there is no swatch matching this layer feature, we can skip this update
            if (!swatches.TryGetValue(layerFeature, out var swatch)) return;
            
            var color = (layer.Styler as CartesianTileLayerStyler).GetColor(layerFeature);

            swatch.SetColor(color.GetValueOrDefault(Color.white));
        }

        private void ShowColorPicker()
        {
            colorPicker.gameObject.SetActive(true);
        }

        private void HideColorPicker()
        {
            colorPicker.gameObject.SetActive(false);
        }

        private bool NoModifierKeyPressed()
        {
            return !LayerUI.AddToSelectionModifierKeyIsPressed() && !LayerUI.SequentialSelectionModifierKeyIsPressed();
        }

        private void ProcessLayerSelection()
        {
            List<ColorSwatch> items = swatches.Values.ToList();
            bool anySelected = false;
            if (LayerUI.SequentialSelectionModifierKeyIsPressed())
            {
                if (selectedItems.Count > 0)
                {                   
                    int firstSelectedIndex = items.IndexOf(selectedItems[0]);
                    int lastSelectedIndex = items.IndexOf(selectedItems[selectedItems.Count - 1]);
                    int targetIndex = currentButtonIndex;
                    int firstIndex = items.IndexOf(firstSelectedItem);

                    bool addSelection = !items[currentButtonIndex].IsSelected;
                    if (!addSelection)
                    {
                        if (firstIndex < targetIndex)
                            for (int i = targetIndex + 1; i <= lastSelectedIndex; i++)
                                items[i].SetSelected(addSelection);
                        else if (firstIndex > targetIndex)
                            for (int i = 0; i < targetIndex; i++)
                                items[i].SetSelected(addSelection);
                        else if (firstIndex == targetIndex)
                            for (int i = 0; i <= lastSelectedIndex; i++)
                                if (i != currentButtonIndex)
                                    items[i].SetSelected(addSelection);
                    }
                    else
                    {
                        //we use the first selected item to only select the range for mutli select and not the last selected item when some are not selected in between
                        if (firstIndex < targetIndex)
                            for (int i = firstIndex; i <= targetIndex; i++)
                                items[i].SetSelected(addSelection);
                        else if (firstIndex > targetIndex)
                            for (int i = targetIndex; i <= firstIndex; i++)
                                items[i].SetSelected(addSelection);
                    }
                }
                else
                {
                    anySelected = true;
                    items[currentButtonIndex].SetSelected(true);
                }
            }
            else if(LayerUI.AddToSelectionModifierKeyIsPressed())
            {
                items[currentButtonIndex].SetSelected(!items[currentButtonIndex].IsSelected);
                if (items[currentButtonIndex].IsSelected)
                {
                    anySelected = true;
                    firstSelectedItem = items[currentButtonIndex];
                }
            }
            if (NoModifierKeyPressed())
            {
                foreach (var item in items)
                    item.SetSelected(false);

                //are we toggling the previous selected only item?
                if (selectedItems.Count != 1 || selectedItems[0] != items[currentButtonIndex])
                {
                    anySelected = true;
                    items[currentButtonIndex].SetSelected(true);
                }
            }
            UpdateSelection();
            if (anySelected)
            {
                //cache the first selected item for sequential selection to always know where to start
                if (selectedItems.Count == 0 || (selectedItems.Count == 1 && firstSelectedItem != items[currentButtonIndex]))
                    firstSelectedItem = items[currentButtonIndex];

                ShowColorPicker();
                colorPicker.PickColorWithoutNotify(items[currentButtonIndex].Color);
            }
            else if (selectedItems.Count == 0)
            {
                HideColorPicker();
            }
        }

        private void UpdateSelection()
        {
            selectedItems.Clear();
            foreach (ColorSwatch item in swatches.Values.ToList())
                if (item.IsSelected)
                    selectedItems.Add(item);
            if (selectedItems.Count == 0)
                firstSelectedItem = null;
        }
    }
}