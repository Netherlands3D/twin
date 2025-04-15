using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
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

        public override LayerGameObject LayerGameObject
        {
            get => layer;
            set
            {
                layer = value;
                layer.OnStylingApplied.AddListener(UpdateSwatchFromStyleChange);
                LoadLayerFeatures();
                StartCoroutine(WaitFrame());
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
                swatch.Button.onClick.AddListener(() => OnClickedSwatch(cachedIndex));
                Material mat = layerFeatures[i].Geometry as Material;
                swatch.SetColor(mat.color);
            }
        }

        private void OnClickedSwatch(int buttonIndex)
        {
            //Debug.Log(buttonIndex);
            CartesianTileLayerGameObject cartesianTileLayerGameObject = layer as CartesianTileLayerGameObject;
            cartesianTileLayerGameObject.Applicator.SetIndex(buttonIndex);            
        }

        private void UpdateSwatchFromStyleChange()
        {
            CartesianTileLayerGameObject cartesianTileLayerGameObject = layer as CartesianTileLayerGameObject;
            ColorSwatch swatch = layerContent.GetChild(cartesianTileLayerGameObject.Applicator.materialIndex).GetComponent<ColorSwatch>();
            swatch.SetColor(cartesianTileLayerGameObject.Applicator.GetMaterial().color);
        }

        private void OnDestroy()
        {
            layer.OnStylingApplied.RemoveListener(UpdateSwatchFromStyleChange);
        }
    }
}