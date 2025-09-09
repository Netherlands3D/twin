using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class HiddenObjectsPropertySection : PropertySectionWithLayerGameObject
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject hiddenItemPrefab;
        [SerializeField] private RectTransform layerContent;

        private LayerGameObject layer;

        private Dictionary<LayerFeature, HiddenObjectsVisibilityItem> hiddenObjects = new();

        public override LayerGameObject LayerGameObject
        {
            get => layer;
            set => Initialize(value);
        }

        private void Initialize(LayerGameObject layer)
        {
            this.layer = layer;
            CreateItems();
            UpdateVisibility();
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
            foreach(var layerFeature in layer.LayerFeatures.Values)
            {
                bool? visibility = (layer.Styler as CartesianTileLayerStyler).GetVisibilityForSubObject(layerFeature);
                if(visibility == false)
                    CreateVisibilityItem(layerFeature);
            }

            //int debugCounter = 0;
            //foreach (var layerFeature in layer.LayerFeatures.Values)
            //{
            //    if(debugCounter < 3 || layerFeature.GetAttribute(CartesianTileLayerStyler.VisibilityIdentifier) == "0307100000333887")
            //        CreateVisibilityItem(layerFeature);

            //    debugCounter++;
            //}            
        }

        private void CreateVisibilityItem(LayerFeature layerFeature)
        {
            if(hiddenObjects.ContainsKey(layerFeature)) return;

            GameObject visibilityObject = Instantiate(hiddenItemPrefab, layerContent);
            string layerName = layerFeature.GetAttribute(CartesianTileLayerStyler.VisibilityIdentifier);
            HiddenObjectsVisibilityItem item = visibilityObject.GetComponent<HiddenObjectsVisibilityItem>();
            item.SetBagId(layerName);
            item.SetLayerFeature(layerFeature);
            //because all ui elements will be destroyed on close an anonymous listener is fine here              
            item.ToggleVisibility.AddListener(visible => SetVisibilityForFeature(layerFeature, visible));

            hiddenObjects.Add(layerFeature, item);
        }

        private void UpdateVisibility()
        {
            foreach (KeyValuePair<LayerFeature, HiddenObjectsVisibilityItem> kv in hiddenObjects)
            {
                bool? visibility = (layer.Styler as CartesianTileLayerStyler).GetVisibilityForSubObject(kv.Key);
                kv.Value.SetToggleState(visibility == true);
            }
        }        

        private void SetVisibilityForFeature(LayerFeature layerFeature, bool visible)
        {
            (layer.Styler as CartesianTileLayerStyler).SetVisibilityForSubObject(layerFeature, visible);
        }
    }
}