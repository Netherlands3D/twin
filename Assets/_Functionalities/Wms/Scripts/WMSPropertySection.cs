using Netherlands3D.Indicators.UI;
using Netherlands3D.Twin.Layers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class WMSPropertySection : MonoBehaviour
    {
        public WMSLayerGameObject Controller
        {
            get
            {
                return controller;
            }
            set
            {
                controller = value;
                controller.LayerData.LayerSelected.AddListener(OnSelectLayer);
                controller.LayerData.LayerDeselected.AddListener(OnDeselectLayer);
            }
        }


        [SerializeField] private GameObject legendPanelPrefab;
        private static GameObject legend;
        [SerializeField] private Vector2Int legendOffsetFromParent;
        private WMSLayerGameObject controller;


        private void Start()
        {
            if (legend == null)
            {
                legend = Instantiate(legendPanelPrefab);
                Inspector inspector = FindObjectOfType<Inspector>();
                legend.transform.SetParent(inspector.Content);
            }
            legend.SetActive(false);
            RectTransform rt = legend.GetComponent<RectTransform>();
            rt.anchoredPosition = legendOffsetFromParent;
            rt.localScale = Vector2.one;
        }

        private void OnSelectLayer(LayerData layer)
        {
            if(legend != null)
                legend.SetActive(true);
        }

        private void OnDeselectLayer(LayerData layer)
        {
            if (legend != null)
                legend.SetActive(false);
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.LayerData.LayerSelected.RemoveListener(OnSelectLayer);
                controller.LayerData.LayerDeselected.RemoveListener(OnDeselectLayer);
            }
        }
    }
}
