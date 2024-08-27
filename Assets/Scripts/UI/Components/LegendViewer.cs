using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class LegendViewer : MonoBehaviour
    {


        [Header("Aspect Ratio Settings")]


        [SerializeField] private float AspectRatio = 0;
        [SerializeField] private GameObject LegendImage;
        [SerializeField] private float ImageAspectRatio = 0;
        [SerializeField] private RectTransform LegendPanel;

        // Start is called before the first frame update
        void Start()
        {

        }

        public void AdaptPanelSize()
        {
            if ((LegendImage.GetComponent<Image>().mainTexture.width / LegendImage.GetComponent<Image>().mainTexture.height) < 0.7f)
            {
                LegendPanel.sizeDelta = new Vector2(400, 600);
            }

            if (((LegendImage.GetComponent<Image>().mainTexture.width / LegendImage.GetComponent<Image>().mainTexture.height) <= 1.5f) && ((LegendImage.GetComponent<Image>().mainTexture.width / LegendImage.GetComponent<Image>().mainTexture.height) >= 0.7f))
            {
                LegendPanel.sizeDelta = new Vector2(400, 400 / (LegendImage.GetComponent<Image>().mainTexture.width / LegendImage.GetComponent<Image>().mainTexture.height));
            }

            if (((LegendImage.GetComponent<Image>().mainTexture.width / LegendImage.GetComponent<Image>().mainTexture.height) > 1.5f))
            {

            }
        }

        // Update is called once per frame
        void Update()
        {
            AdaptPanelSize();
        }
    }
}


