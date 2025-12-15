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
    [PropertySection(typeof(LayerFeatureColorPropertyData))]
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

            //TODO this could be personal, but a hunch these (runtime only) layerfeatures should be part of a data container so this propertysection and other logic should not be visualisation dependent
            CartesianTileLayerGameObject visualization = FindObjectsByType<CartesianTileLayerGameObject>(FindObjectsSortMode.None).ToList()
                .FirstOrDefault(v => v.LayerData.GetProperty<LayerFeatureColorPropertyData>() == stylingPropertyData);

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
                
            string layerName = layerFeature.GetAttribute(LayerFeatureColorPropertyData.MaterialNameIdentifier);
                
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
                stylingPropertyData.SetColor(layerFeature, color);
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
            
            var color = stylingPropertyData.GetColor(layerFeature);

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