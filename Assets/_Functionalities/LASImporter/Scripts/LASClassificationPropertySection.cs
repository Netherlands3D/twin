using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.LASImporter
{
    [PropertySection(typeof(LASClassificationColorPropertyData))]
    public class LASClassificationPropertySection : MonoBehaviour, IVisualizationWithPropertyData, IMultiSelectable
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject colorSwatchPrefab;
        [SerializeField] private RectTransform layerContent;
        [SerializeField] private ColorPickerPropertySection colorPicker;

        private readonly Dictionary<byte, ColorSwatch> swatches = new();
        private LASClassificationColorPropertyData propertyData;

        public int SelectedButtonIndex { get; set; } = -1;
        public List<ISelectable> SelectedItems { get; } = new();
        public List<ISelectable> Items { get; set; } = new();
        public ISelectable FirstSelectedItem { get; set; }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            propertyData = properties.GetDefaultStylingPropertyData<LASClassificationColorPropertyData>();
            if (propertyData == null)
                return;

            if (!layerContent || !colorSwatchPrefab || !colorPicker)
            {
                Debug.LogError("LAS classification property section is missing prefab references.", this);
                return;
            }

            CreateSwatches();
            propertyData.OnStylingChanged.AddListener(UpdateSwatches);
            colorPicker.ColorWheel.colorChanged.AddListener(OnPickColor);
            StartCoroutine(OnPropertySectionsLoaded());
        }

        private void OnDestroy()
        {
            propertyData?.OnStylingChanged.RemoveListener(UpdateSwatches);
            if (colorPicker)
                colorPicker.ColorWheel.colorChanged.RemoveListener(OnPickColor);
        }

        private IEnumerator OnPropertySectionsLoaded()
        {
            yield return new WaitForEndOfFrame();

            HideColorPicker();
            if (TryGetComponent<LayoutElement>(out var layout) && content)
                layout.minHeight = content.rect.height;
        }

        private void CreateSwatches()
        {
            swatches.Clear();
            layerContent.ClearAllChildren();

            foreach (var classification in propertyData.GetClassifications())
            {
                swatches[classification] = CreateSwatch(classification);
                SetSwatchColor(classification);
            }

            Items = swatches.Values.OfType<ISelectable>().ToList();
        }

        private ColorSwatch CreateSwatch(byte classification)
        {
            var swatchObject = Instantiate(colorSwatchPrefab, layerContent);
            var swatch = swatchObject.GetComponent<ColorSwatch>();
            var count = propertyData.GetCount(classification);
            var label = $"{propertyData.GetClassificationName(classification)} ({count})";

            swatch.SetLayerName(label);
            swatch.SetInputText(label);
            swatch.onClickDown.AddListener(pointer => OnClickedOnSwatch(pointer, swatch));

            return swatch;
        }

        private void OnClickedOnSwatch(PointerEventData _, ColorSwatch swatch)
        {
            SelectedButtonIndex = Items.IndexOf(swatch);
            MultiSelectionUtility.ProcessLayerSelection(this, anySelected =>
            {
                if (anySelected)
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
            foreach (var (classification, swatch) in swatches)
            {
                if (!swatch.IsSelected) continue;

                swatch.SetColor(color);
                propertyData.SetColorByClassification(classification, propertyData.GetClassificationName(classification), color);
            }
        }

        private void UpdateSwatches()
        {
            if (swatches.Count != propertyData.ClassificationCounts.Count)
            {
                CreateSwatches();
                return;
            }

            foreach (var classification in swatches.Keys)
            {
                SetSwatchColor(classification);
            }
        }

        private void SetSwatchColor(byte classification)
        {
            if (!swatches.TryGetValue(classification, out var swatch)) return;

            var color = propertyData.GetColorByClassification(classification);
            swatch.SetColor(color.GetValueOrDefault(Color.white));
        }

        private void ShowColorPicker()
        {
            colorPicker.gameObject.SetActive(true);
            colorPicker.LoadProperties(new List<LayerPropertyData> { propertyData });
        }

        private void HideColorPicker()
        {
            colorPicker.gameObject.SetActive(false);
        }
    }
}
