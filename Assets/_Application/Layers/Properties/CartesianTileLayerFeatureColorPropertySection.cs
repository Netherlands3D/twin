using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
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
            if (swatch.IsSelected)
            {
                DeselectAllSwatches();
                DeselectSwatch(swatch);
                return;
            }

            DeselectAllSwatches();
            SelectSwatch(swatch);
        }

        private void SelectSwatch(ColorSwatch swatch)
        {
            ShowColorPicker();
            colorPicker.PickColorWithoutNotify(swatch.Color);

            swatch.SetSelected(true);
        }

        private void DeselectAllSwatches()
        {
            foreach (var (_, swatch) in swatches)
            {
                if (swatch.IsSelected) continue;
                
                DeselectSwatch(swatch);
            }
        }

        private void DeselectSwatch(ColorSwatch swatch)
        {
            swatch.SetSelected(false);
            
            HideColorPicker();
        }

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
            CartesianTileLayerStyler.SetColor(layer, layerFeature, color);
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

            var color = CartesianTileLayerStyler.GetColor(layer, layerFeature);

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
    }
}