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
            List<LayerFeature> layerFeatures = layer.GetLayerFeatures().Values.ToList();
            for(int i = 0; i < layerFeatures.Count; i++) 
            {
                GameObject swatch = Instantiate(colorSwatchPrefab, layerContent);
                swatch.GetComponentInChildren<TMP_InputField>().text = layerFeatures[i].Attributes.Values.FirstOrDefault().ToString();
                int cachedIndex = i;
                swatch.GetComponent<Button>().onClick.AddListener(() => OnClickedSwatch(cachedIndex));
            }
        }

        private void OnClickedSwatch(int buttonIndex)
        {
            //Debug.Log(buttonIndex);
            CartesianTileLayerGameObject cartesianTileLayerGameObject = layer as CartesianTileLayerGameObject;
            cartesianTileLayerGameObject.Applicator.SetIndex(buttonIndex);            
        }

       
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