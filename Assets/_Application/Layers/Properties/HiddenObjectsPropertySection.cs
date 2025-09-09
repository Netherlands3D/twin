using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class HiddenObjectsPropertySection : PropertySectionWithLayerGameObject
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject hiddenItemPrefab;
        [SerializeField] private RectTransform layerContent;

        private LayerGameObject layer;

        public override LayerGameObject LayerGameObject
        {
            get => layer;
            set => Initialize(value);
        }

        private void Initialize(LayerGameObject layer)
        {
            this.layer = layer;
            CreateItems();
            layer.OnStylingApplied.AddListener(UpdateVisibility);

            StartCoroutine(OnPropertySectionsLoaded());
        }

        private void OnDestroy()
        {
            layer.OnStylingApplied.RemoveListener(UpdateVisibility);
        }

        private IEnumerator OnPropertySectionsLoaded()
        {
            yield return new WaitForEndOfFrame();
                       
            // workaround to have a minimum height for the content loaded (because of scrollrects)
            LayoutElement layout = GetComponent<LayoutElement>();
            layout.minHeight = content.rect.height;
        }

        private void CreateItems()
        {
            layerContent.ClearAllChildren();
            foreach (var layerFeature in layer.LayerFeatures.Values)
            {
                //create list item
            }
        }

        //private ColorSwatch CreateSwatch(LayerFeature layerFeature)
        //{
        //    GameObject swatchObject = Instantiate(colorSwatchPrefab, layerContent);
        //    ColorSwatch swatch = swatchObject.GetComponent<ColorSwatch>();

        //    string layerName = layerFeature.GetAttribute(CartesianTileLayerStyler.MaterialNameIdentifier);

        //    swatch.SetLayerName(layerName);
        //    swatch.SetInputText(layerName);

        //    //because all ui elements will be destroyed on close an anonymous listener is fine here              
        //    swatch.onClickDown.AddListener(pointer => OnClickedOnSwatch(pointer, swatch));

        //    return swatch;
        //}

        //private void OnClickedOnSwatch(PointerEventData _, ColorSwatch swatch)
        //{
        //    if (swatch.IsSelected)
        //    {
        //        DeselectAllSwatches();
        //        DeselectSwatch(swatch);
        //        return;
        //    }

        //    DeselectAllSwatches();
        //    SelectSwatch(swatch);
        //}

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

        //private void OnPickColor(Color color)
        //{
        //    foreach ((LayerFeature layerFeature, ColorSwatch swatch) in swatches)
        //    {
        //        if (!swatch.IsSelected) continue;

        //        swatch.SetColor(color);
        //        SetColorizationStylingRule(layerFeature, color);
        //    }
        //}

        //private void SetColorizationStylingRule(LayerFeature layerFeature, Color color)
        //{
        //    (layer.Styler as CartesianTileLayerStyler).SetColor(layerFeature, color);
        //}

        private void UpdateVisibility()
        {
            foreach (KeyValuePair<object, LayerFeature> kv in layer.LayerFeatures)
            {
                SetVisibilityFromFeature(kv.Value);
            }
        }

        private void SetVisibilityFromFeature(LayerFeature layerFeature)
        {          
            var visibility = (layer.Styler as CartesianTileLayerStyler).GetVisibilityForSubObject(layerFeature);
            //todo : set visibility on UI element
        }
    }
}