using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    [PropertySection(typeof(LayerFeatureColorPropertyData))]
    public class CartesianTileLayerFeatureColorPropertySection : MonoBehaviour, IVisualizationWithPropertyData, IMultiSelectable
    {  
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject colorSwatchPrefab;
        [SerializeField] private RectTransform layerContent;

        private readonly Dictionary<int, ColorSwatch> swatches = new();
        [SerializeField] private ColorPickerPropertySection colorPicker;

        public int SelectedButtonIndex { get; set; } = -1;
        public List<ISelectable> SelectedItems { get; } = new();
        public List<ISelectable> Items { get; set; } = new();
        public ISelectable FirstSelectedItem { get; set; }

        private LayerFeatureColorPropertyData stylingPropertyData;

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            stylingPropertyData = properties.GetDefaultStylingPropertyData<LayerFeatureColorPropertyData>(); 
            
            CreateSwatches();

            stylingPropertyData.OnStylingChanged.AddListener(UpdateSwatches);
            colorPicker.ColorWheel.colorChanged.AddListener(OnPickColor);

            StartCoroutine(OnPropertySectionsLoaded());
        }

        private void OnDestroy()
        {
            stylingPropertyData.OnStylingChanged.RemoveListener(UpdateSwatches);
            colorPicker.ColorWheel.colorChanged.RemoveListener(OnPickColor);
        }

        private IEnumerator OnPropertySectionsLoaded()
        {
            yield return new WaitForEndOfFrame(); 
            
            HideColorPicker();
            // workaround to have a minimum height for the content loaded (because of scrollrects)
            LayoutElement layout = GetComponent<LayoutElement>();
            layout.minHeight = content.rect.height;
        }

        private void CreateSwatches()
        {
            swatches.Clear();
            layerContent.ClearAllChildren();
            
            foreach(KeyValuePair<string, StylingRule> kv in stylingPropertyData.StylingRules)
            {
                if(kv.Key.Contains(LayerFeatureColorPropertyData.ColoringIdentifier))
                {
                    int index = stylingPropertyData.GetMaterialIndexFromStyleRuleKey(kv.Key);                    
                    Color? color = stylingPropertyData.GetColorByMaterialIndex(index);
                    //we need to expect a value here or else the stylingrule is not properly initialized
                    if (color.HasValue)
                    {
                        swatches[index] = CreateSwatch(index);
                        SetSwatchColorFromFeature(index);
                    }
                    else
                        Debug.LogError("stylingrule not initialized because the colorvalue is missing");
                }
            }
            Items = swatches.Values.OfType<ISelectable>().ToList();
        }

        private ColorSwatch CreateSwatch(int index)
        {
            GameObject swatchObject = Instantiate(colorSwatchPrefab, layerContent);
            ColorSwatch swatch = swatchObject.GetComponent<ColorSwatch>();

            string layerName = stylingPropertyData.GetStylingRuleNameByMaterialIndex(index);
                
            swatch.SetLayerName(layerName);
            swatch.SetInputText(layerName);

            //because all ui elements will be destroyed on close an anonymous listener is fine here              
            swatch.onClickDown.AddListener(pointer => OnClickedOnSwatch(pointer, swatch));

            return swatch;
        }

        private void OnClickedOnSwatch(PointerEventData _, ColorSwatch swatch)
        {
            //select layer
            SelectedButtonIndex = Items.IndexOf(swatch);
            MultiSelectionUtility.ProcessLayerSelection(this, anySelected =>
            {
                if(anySelected)
                {
                    ShowColorPicker();
                    colorPicker.PickColorWithoutNotify(((ColorSwatch)Items[SelectedButtonIndex]).Color);
                }
                else
                {
                    HideColorPicker();
                }
            });
        }

        private void OnPickColor(Color color)
        {
            foreach ((int index, ColorSwatch swatch) in swatches)
            {
                if (!swatch.IsSelected) continue;
                
                swatch.SetColor(color);
                stylingPropertyData.SetColorByMaterialIndex(index, swatch.LayerName, color);
            }
        }

        private void UpdateSwatches()
        {
            foreach (var (layerFeature, _) in swatches)
            {
                SetSwatchColorFromFeature(layerFeature);
            }
        }

        private void SetSwatchColorFromFeature(int index)
        {
            // if there is no swatch matching this layer feature, we can skip this update
            if (!swatches.TryGetValue(index, out var swatch)) return;
            
            var color = stylingPropertyData.GetColorByMaterialIndex(index);

            swatch.SetColor(color.GetValueOrDefault(Color.white));
        }

        private void ShowColorPicker()
        {
            colorPicker.gameObject.SetActive(true);
            colorPicker.LoadProperties(new List<LayerPropertyData>() { stylingPropertyData });
        }

        private void HideColorPicker()
        {
            colorPicker.gameObject.SetActive(false);
        }       
    }
}