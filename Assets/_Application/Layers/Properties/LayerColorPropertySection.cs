using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.LayerStyles;
using Netherlands3D.LayerStyles.Expressions;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.UI.HierarchyInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class LayerColorPropertySection : PropertySectionWithLayerGameObject
    {  
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject colorSwatchPrefab;
        [SerializeField] private RectTransform layerContent;

        private LayerGameObject layer;
        private readonly Dictionary<LayerFeature, ColorSwatch> swatches = new();
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
            CreateSwatches();
            StartCoroutine(OnPropertySectionsLoaded()); 
        }

        private IEnumerator OnPropertySectionsLoaded()
        {
            yield return new WaitForEndOfFrame();

            // workaround to have a minimum height for the content loaded (because of scrollrects)
            LayoutElement layout = GetComponent<LayoutElement>();
            layout.minHeight = content.rect.height;

            // we need to wait a frame for this so all propertysections will be available after instantiation
            // TODO: Why FindAny? Shouldn't this be a direct link? And the colorpicker should only be shown when a layer is selected
            colorPicker = FindAnyObjectByType<ColorPickerPropertySection>();
        }

        private void CreateSwatches()
        {
            swatches.Clear();
            layerContent.ClearAllChildren();
            foreach (var layerFeature in layer.GetLayerFeatures().Values)
            {
                swatches[layerFeature] = CreateSwatch(layerFeature);
            }
        }

        private ColorSwatch CreateSwatch(LayerFeature layerFeature)
        {
            GameObject swatchObject = Instantiate(colorSwatchPrefab, layerContent);
            ColorSwatch swatch = swatchObject.GetComponent<ColorSwatch>();
                
            string layerName = layerFeature.GetAttribute(Constants.MaterialNameIdentifier);
                
            swatch.SetLayerName(layerName);
            swatch.SetInputText(layerName);

            //because all ui elements will be destroyed on close an anonymous listener is fine here              
            swatch.onClickDown.AddListener(pointer => OnSelectedSwatch(pointer, swatch));

            SetSwatchColorFromFeature(layerFeature, swatch);

            return swatch;
        }

        private void OnSelectedSwatch(PointerEventData _, ColorSwatch swatch)
        {
            ProcessLayerSelection();
            SelectSwatch(swatch, !swatch.IsSelected);
        }

        private void SelectSwatch(ColorSwatch swatch, bool select)
        {
            if (select)
            {
                colorPicker.SetColorPickerColor(swatch.Color);
            }

            swatch.SetSelected(select);
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            foreach ((LayerFeature layerFeature, ColorSwatch swatch) in swatches)
            {
                if (!swatch.IsSelected) continue;
                
                // TODO: Why is this here? And not in response to the color picked event?
                SetColorizationStylingRule(layerFeature, swatch.Color);
            }
        }

        private void SetColorizationStylingRule(LayerFeature layerFeature, Color color)
        {
            int.TryParse(layerFeature.Attributes[Constants.MaterialIndexIdentifier], out int materialIndexIdentifier);

            var stylingRuleName = ColorizationStyleRuleName(materialIndexIdentifier);

            // Add or set the colorization of this feature by its material index
            var stylingRule = new StylingRule(
                stylingRuleName, 
                Expr.EqualsTo(
                    Expr.GetVariable(Constants.MaterialIndexIdentifier),
                    materialIndexIdentifier
                )
            );
            stylingRule.Symbolizer.SetFillColor(color);
                
            layer.LayerData.DefaultStyle.StylingRules[stylingRuleName] = stylingRule;
        }

        private static string ColorizationStyleRuleName(int materialIndexIdentifier)
        {
            return $"feature.{materialIndexIdentifier}.colorize";
        }

        private void UpdateSwatchFromStyleChange()
        {
            CartesianTileLayerGameObject cartesianTileLayerGameObject = layer as CartesianTileLayerGameObject;
            foreach (var kv in cartesianTileLayerGameObject.GetLayerFeatures())
            {
                var layerFeature = kv.Value;
                if (!int.TryParse(layerFeature.Attributes[Constants.MaterialIndexIdentifier], out int materialIndexIdentifier))
                    continue;

                SetSwatchColorFromFeature(
                    layerFeature, 
                    layerContent.GetChild(materialIndexIdentifier).GetComponent<ColorSwatch>()
                );
            }
        }

        private void SetSwatchColorFromFeature(LayerFeature layerFeature, ColorSwatch swatch)
        {
            int.TryParse(layerFeature.GetAttribute(Constants.MaterialIndexIdentifier), out int materialIndexIdentifier);
            var stylingRuleName = ColorizationStyleRuleName(materialIndexIdentifier);
            swatch.SetColor(((Material)layerFeature.Geometry).color); // TODO: This can be done better

            if (!layer.LayerData.DefaultStyle.StylingRules.TryGetValue(stylingRuleName, out var stylingRule)) return;

            Color? color = stylingRule.Symbolizer.GetFillColor();

            swatch.SetColor(color.GetValueOrDefault(Color.white));
        }

        private void OnDestroy()
        {
            layer.OnStylingApplied.RemoveListener(UpdateSwatchFromStyleChange);
        }

        private void ProcessLayerSelection()
        {
            // if (LayerUI.SequentialSelectionModifierKeyIsPressed() && selectedSwatches.Count > 0) //if no layers are selected, there will be no reference layer to add to
            // {                
            //     int lastIndex = items.IndexOf(selectedSwatches[selectedSwatches.Count - 1]); //last element is always the last selected layer                               
            //     int targetIndex = currentButtonIndex;
            //     if(lastIndex > targetIndex)
            //     {
            //         int temp = lastIndex;
            //         lastIndex = targetIndex;
            //         targetIndex = temp;
            //     }
            //     bool addSelection = !items[currentButtonIndex].IsSelected; 
            //     for (int i = lastIndex; i <= targetIndex; i++)
            //     {
            //         items[i].SetSelected(addSelection);
            //     }
            //
            //     items[currentButtonIndex].SetSelected(!addSelection);
            // }
            
            if (NoModifierKeyPressed())
            {
                DeselectAllSwatches();
            }

            UpdateSelection();
        }

        private void DeselectAllSwatches()
        {
            swatches.Values
                .Where(swatch => swatch.IsSelected)
                .ToList()
                .ForEach(swatch => swatch.SetSelected(false));
        }

        private bool NoModifierKeyPressed()
        {
            return !LayerUI.AddToSelectionModifierKeyIsPressed() && !LayerUI.SequentialSelectionModifierKeyIsPressed();
        }
    }
}