using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    [PropertySection(typeof(StylingPropertyData), CartesianTileLayerStyler.LayerFeatureColoring)]// Symbolizer.FillColorProperty)]
    public class CartesianTileLayerFeatureColorPropertySection : MonoBehaviour, IVisualizationWithPropertyData, IMultiSelectable
    {  
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject colorSwatchPrefab;
        [SerializeField] private RectTransform layerContent;

        private readonly Dictionary<LayerFeature, ColorSwatch> swatches = new();
        [SerializeField] private ColorPickerPropertySection colorPicker;

        public int SelectedButtonIndex { get; set; } = -1;
        public List<ISelectable> SelectedItems { get; } = new();
        public List<ISelectable> Items { get; set; } = new();
        public ISelectable FirstSelectedItem { get; set; }

        private StylingPropertyData stylingPropertyData;

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            stylingPropertyData = properties.Get<StylingPropertyData>();
           

            CreateSwatches();

            stylingPropertyData.OnStylingApplied.AddListener(UpdateSwatches);

            StartCoroutine(OnPropertySectionsLoaded());
        }

        private void OnDestroy()
        {
            stylingPropertyData.OnStylingApplied.RemoveListener(UpdateSwatches);
            colorPicker.ColorWheel.colorChanged.RemoveListener(OnPickColor);
        }

        private IEnumerator OnPropertySectionsLoaded()
        {
            yield return new WaitForEndOfFrame();

            // Reset listeners to prevent default behaviour
            colorPicker.ColorWheel.colorChanged.RemoveAllListeners();
            //colorPicker.LayerGameObject = layer;
            colorPicker.ColorWheel.colorChanged.AddListener(OnPickColor);
            HideColorPicker();

            // workaround to have a minimum height for the content loaded (because of scrollrects)
            LayoutElement layout = GetComponent<LayoutElement>();
            layout.minHeight = content.rect.height;
        }

        private void CreateSwatches()
        {
            swatches.Clear();
            layerContent.ClearAllChildren();


            CartesianTileLayerGameObject visualization = FindObjectsByType<CartesianTileLayerGameObject>(FindObjectsSortMode.None).ToList()
                .FirstOrDefault(v => v.LayerData.GetProperty<StylingPropertyData>() == stylingPropertyData);

            if(visualization == null) 
            {
                Debug.LogError("invalid visualisation!");
                return;
            }

            foreach (var layerFeature in visualization.LayerFeatures.Values)
            {
                if (layerFeature.Geometry is not Material) continue;

                swatches[layerFeature] = CreateSwatch(layerFeature);
                SetSwatchColorFromFeature(layerFeature);
            }
            Items = swatches.Values.OfType<ISelectable>().ToList();
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
            foreach ((LayerFeature layerFeature, ColorSwatch swatch) in swatches)
            {
                if (!swatch.IsSelected) continue;
                
                swatch.SetColor(color);
                CartesianTileLayerStyler.SetColor(layerFeature, color, stylingPropertyData);
            }
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
            
            var color = CartesianTileLayerStyler.GetColor(layerFeature, stylingPropertyData);

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