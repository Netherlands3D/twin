using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class stempelkaart : MonoBehaviour
    {
        [SerializeField] private Sprite vol;
        [SerializeField] private GameObject one;
        [SerializeField] private GameObject two;
        [SerializeField] private GameObject three;
        [SerializeField] private Sprite empty;
        [SerializeField] private GameObject stamp;

        private Image img;


        // Start is called before the first frame update
        void Start()
        {
            img = GetComponent<Image>();
            img.sprite = empty;

            SetStampEnabled(1, false);
            SetStampEnabled(2, false);
            SetStampEnabled(3, false);
        }

        public void SetStampEnabled(int num, bool enabled)
        {
            RectTransform rect = stamp.GetComponent<RectTransform>();
            if (num == 1)
            {
                one.SetActive(enabled);
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, one.GetComponent<RectTransform>().anchoredPosition.y + 30);
            }
            if (num == 2)
            {
                two.SetActive(enabled);
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, two.GetComponent<RectTransform>().anchoredPosition.y + 30);
            }
            if (num == 3)
            {
                three.SetActive(enabled);
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, three.GetComponent<RectTransform>().anchoredPosition.y + 30);
            }
        }

        public void SetStampMarkerEnabled(bool enabled)
        {
            stamp.SetActive(enabled);
        }
    }
}
