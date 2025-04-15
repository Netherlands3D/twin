using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.UI.ColorPicker;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
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

        private void Start()
        {
            LoadLayerFeatures();
            StartCoroutine(WaitFrame());
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
            Dictionary<object, LayerFeature> layerFeatures = layer.GetLayerFeatures();
            foreach(KeyValuePair<object, LayerFeature> kv in layerFeatures)
            {
                GameObject swatch = Instantiate(colorSwatchPrefab, layerContent);
                swatch.GetComponentInChildren<TMP_InputField>().text = kv.Value.Attributes.Values.FirstOrDefault().ToString();
            }
        }

        //features ophalen op basis van de feature materials en ui swatches aanmaken met juiste namen
        //selectie styling rule -> colorwheel styling rule zetten
        //koppeling met de colorwheel
        //apply op layer om te kleuren


        public override LayerGameObject LayerGameObject
        {
            get => layer;
            set
            {
                layer = value;
                //colorPicker.SetColorWithoutNotify(layer.LayerData.DefaultStyle.AnyFeature.Symbolizer.GetFillColor() ?? defaultColor);
            }
        }

        //private void OnEnable()
        //{
        //    colorPicker.colorChanged.AddListener(OnPickedColor);
        //}

        //private void OnDisable()
        //{
        //    colorPicker.colorChanged.RemoveListener(OnPickedColor);
        //}

        //public void OnPickedColor(Color color)
        //{
        //    layer.LayerData.DefaultStyle.AnyFeature.Symbolizer.SetFillColor(color);
        //    layer.ApplyStyling();
        //}


    }
}