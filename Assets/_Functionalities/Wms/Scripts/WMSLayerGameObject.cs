using KindMen.Uxios;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.UI.LayerInspector;
using Netherlands3D.Web;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers
{
    /// <summary>
    /// Extention of LayerGameObject that injects a 'streaming' dataprovider WMSTileDataLayer
    /// </summary>
    public class WMSLayerGameObject : CartesianTilePropertyLayer, ILayerWithPropertyData
    {
        public WMSTileDataLayer WMSProjectionLayer => wmsProjectionLayer;       
        public bool TransparencyEnabled = true; //this gives the requesting url the extra param to set transparancy enabled by default       
        public int DefaultEnabledLayersMax = 5;  //in case the dataset is very large with many layers. lets topggle the layers after this count to not visible.
        public Vector2Int PreferredImageSize = Vector2Int.one * 512;
        public LayerPropertyData PropertyData => urlPropertyData;

        private WMSTileDataLayer wmsProjectionLayer;
        protected LayerURLPropertyData urlPropertyData = new();

        [SerializeField] private GameObject legendPanelPrefab;
        private static GameObject legend;
        private Texture2D legendImage;
        [SerializeField] private Vector2Int legendOffsetFromParent;



        protected override void Awake()
        {
            base.Awake();
            wmsProjectionLayer = GetComponent<WMSTileDataLayer>();
            LayerData.LayerSelected.AddListener(OnSelectLayer);
            LayerData.LayerDeselected.AddListener(OnDeselectLayer);
        }

        protected override void Start()
        {
            base.Start();
            WMSProjectionLayer.WmsUrl = urlPropertyData.Data.ToString();
            
            LayerData.LayerOrderChanged.AddListener(SetRenderOrder);

            SetRenderOrder(LayerData.RootIndex);

            if (legend == null)
            {
                legend = Instantiate(legendPanelPrefab);
                Inspector inspector = FindObjectOfType<Inspector>();
                legend.transform.SetParent(inspector.Content);
            }
            RectTransform rt = legend.GetComponent<RectTransform>();
            rt.anchoredPosition = legendOffsetFromParent;
            rt.localScale = Vector2.one;

            //?SERVICE=WMS&VERSION=1.1.1&REQUEST=GetLegendGraphic&FORMAT=image/png&LAYER={layer}";
            var parameters = QueryString.Decode(urlPropertyData.Data.Query.TrimStart('?'));
            var service = parameters.Get("service"); 
            var version = parameters.Get("version");             
            var layers = parameters.Get("layers");
            var legendUri = new UriBuilder(urlPropertyData.Data.GetLeftPart(UriPartial.Path));
            legendUri.SetQueryParameter("service", service);
            legendUri.SetQueryParameter("version", "");
            legendUri.SetQueryParameter("request", "getlegendgraphic");
            legendUri.SetQueryParameter("format", "image/png");
            legendUri.SetQueryParameter("layer", layers);
            StartCoroutine(GetLegendGraphic(legendUri.Uri.ToString()));
        }

        public void SetLegendImage(Texture2D legendImage)
        {
            this.legendImage = legendImage;
            LegendImage imageObject = legend.GetComponentInChildren<LegendImage>(true);
            if (legendImage == null)
            {
                imageObject.gameObject.SetActive(false);
                return;
            }
            imageObject.gameObject.SetActive(true);
            Sprite imageSprite = Sprite.Create(legendImage, new Rect(0f, 0f, legendImage.width, legendImage.height), Vector2.one * 0.5f, 100);
            imageObject.GetComponent<Image>().sprite = imageSprite;
        }

        private IEnumerator GetLegendGraphic(string url)
        {
            UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url);
            yield return webRequest.SendWebRequest();
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Could not download {url}");
                SetLegendImage(null);
            }
            else
            {
                Texture texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;
                Texture2D tex = texture as Texture2D;
                //tex.Compress(true);
                //tex.filterMode = FilterMode.Bilinear;
                tex.Apply(false, true);
                SetLegendImage(tex);
            }
        }

        //a higher order means rendering over lower indices
        public void SetRenderOrder(int order)
        {
            //we have to flip the value because a lower layer with a higher index needs a lower render index
            WMSProjectionLayer.RenderIndex = -order;
        }

        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                this.urlPropertyData = urlProperty;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            LayerData.LayerOrderChanged.RemoveListener(SetRenderOrder);
            LayerData.LayerSelected.RemoveListener(OnSelectLayer);
            LayerData.LayerDeselected.RemoveListener(OnDeselectLayer);
        }

        private void OnSelectLayer(LayerData layer)
        {
            if (legend != null)
                legend.SetActive(true);
        }

        private void OnDeselectLayer(LayerData layer)
        {
            if (legend != null)
                legend.SetActive(false);
        }
    }
}