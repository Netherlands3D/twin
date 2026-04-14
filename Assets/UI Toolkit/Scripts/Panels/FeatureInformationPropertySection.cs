using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Rendering;
using Netherlands3D.Twin.Utility;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using ListView = Netherlands3D.UI.Components.ListView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(FeaturePropertyData))]
    public partial class FeatureInformationPropertySection : VisualElement, IVisualizationWithPropertyData
    { 
        private FeaturePropertyData featurePropertyData;
        private Coroutine downloadProcess;
        private VisualElement thumbnailContainer;
        
        private ListView propertiesListView;
        private ListView PropertysListView => propertiesListView ??= this.Q<ListView>();

        private Dictionary<string, object> empty = new() {{ "geen informatie", null} };

        public FeatureInformationPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            thumbnailContainer = this.Q<VisualElement>("ThumbnailContainer");
            
            PropertysListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            PropertysListView.selectionType = SelectionType.None;
            
            PropertysListView.makeItem = MakeListViewItem;
            PropertysListView.bindItem = BindListViewItem;
            
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                featurePropertyData.OnIdsChanged.RemoveListener(OnIdsChanged);
            });
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            featurePropertyData = properties.Get<FeaturePropertyData>();
            featurePropertyData.OnIdsChanged.AddListener(OnIdsChanged);
            
            Dictionary<string, (BoundingBox, Dictionary<string, object>)> featureIds = featurePropertyData.FeatureIds;
            if (featureIds == null || featureIds.Count == 0)
            {
                Clear();
                return;
            }
            LoadFeatureProperties(featureIds);
        }

        private void OnIdsChanged(Dictionary<string, (BoundingBox, Dictionary<string, object>)> featureIds)
        {
            if (featureIds == null || featureIds.Count == 0)
            {
                Clear();
                return;
            }
            LoadFeatureProperties(featureIds);
        }
        
        public void PopulateAddresses(Dictionary<string, object> properties)
        {
            var list = properties
                .Select(kv => new KeyValue
                {
                    Key = kv.Key,
                    Value = kv.Value?.ToString()
                })
                .ToList();
            PropertysListView.itemsSource = list;
            //PropertysListView.RefreshItems();
        }
        
        private VisualElement MakeListViewItem()
        {
            KeyValue kv = new KeyValue();
            kv.ShowDivider(true);
            return kv;
        }
        
        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not KeyValue element) return;
            
            var kv = (KeyValue)PropertysListView.itemsSource[index];
            element.Key = kv.Key;
            element.Value = kv.Value;
        }
        
        private void LoadFeatureProperties(Dictionary<string, (BoundingBox, Dictionary<string, object>)> featureIds)
        {
            foreach (KeyValuePair<string, (BoundingBox, Dictionary<string, object>)> kv in featureIds)
            {
                Dictionary<string, object> featureProperties = kv.Value.Item2;
                BoundingBox bbox = kv.Value.Item1;
                if (downloadProcess != null)
                {
                    ThumbnailCoroutineRunner.Instance.StopCoroutine(downloadProcess);
                }
                downloadProcess = ThumbnailCoroutineRunner.Instance.StartCoroutine(GetFeatureThumbnail(bbox));
                PopulateAddresses(featureProperties);
                break;
            }
        }

        private IEnumerator GetFeatureThumbnail(BoundingBox bbox)
        {
            yield return null;

            //TODO: Use bbox and geometry.coordinates from GeoJSON object to create bounds to render thumbnail
            Bounds currentObjectBounds = bbox.ToUnityBounds();
            RenderTexture rTex = RenderedThumbnail.RenderThumbnail(currentObjectBounds);
            Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGBA32, false);

            RenderTexture.active = rTex;
            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            thumbnailContainer.style.backgroundImage = new StyleBackground(tex);
            float aspect = (float)rTex.height / rTex.width;
            float newHeight = thumbnailContainer.resolvedStyle.width * aspect;
            thumbnailContainer.style.height = newHeight;
        }

        private void Clear()
        {
            thumbnailContainer.style.height = 0;
            PopulateAddresses(empty);
        }
    }
}