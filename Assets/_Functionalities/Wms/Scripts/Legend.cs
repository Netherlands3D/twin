using Netherlands3D.Twin.Layers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Netherlands3D.Twin
{
    public class Legend : MonoBehaviour
    {
        [SerializeField] private RectTransform inactive;
        [SerializeField] private LegendImage graphicPrefab;

        public LayerData CurrentLayer { get; set; }

        private List<LegendImage> graphics = new List<LegendImage>();

        public void AddGraphic(Sprite sprite)
        {
            LegendImage image = Instantiate(graphicPrefab, graphicPrefab.transform.parent);
            image.gameObject.SetActive(true);
            image.SetSprite(sprite);
            graphics.Add(image);

            try
            {
                GetComponentInChildren<LegendClampHeight>().Invoke("AdjustRectHeight", 0);
                GetComponent<ContentFitterRefresh>().Invoke("RefreshContentFitters", 0);
            }
            catch (Exception e)
            {
                print("error");
            }


        }

        public void ClearGraphics()
        {
            if (graphics.Count == 0)
                return;

            for(int i = graphics.Count - 1; i >= 0; i--)
            {
                Destroy(graphics[i].gameObject);
            }
            graphics.Clear();
        }

        public void ShowInactive(bool show)
        {
            inactive.gameObject.SetActive(show);
        }

    }
}
