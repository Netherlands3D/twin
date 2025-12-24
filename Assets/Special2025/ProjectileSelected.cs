using System;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D
{
    public class ProjectileSelected : MonoBehaviour
    {
        public Button next => nextButton;
        public Button previous => previousButton;
        public Image Powericon => powerIcon;
        
        [SerializeField] private Image projectileIcon;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button previousButton;
        [SerializeField] private Image powerIcon;
        
        private PrefabThumbnail pt;

        private void Awake()
        {
            pt = GetComponent<PrefabThumbnail>();
        }

        public void SetImage(object projectilePrefab)
        {
            Sprite sprite = projectilePrefab as Sprite;
            if(sprite != null)
            {
                projectileIcon.sprite = sprite;
                return;
            }

            projectileIcon.sprite = pt.GetThumbnail(projectilePrefab as GameObject);
        }

        public void SetImageForNext(object projectilePrefab)
        {
            Sprite sprite = projectilePrefab as Sprite;
            if (sprite != null)
            {
                nextButton.gameObject.transform.GetChild(0).gameObject.GetComponent<Image>().sprite = sprite;
                return;
            }

            nextButton.gameObject.transform.GetChild(0).gameObject.GetComponent<Image>().sprite = pt.GetThumbnail(projectilePrefab as GameObject);
        }
        
        public void SetImageForPrevious(object projectilePrefab)
        {
            Sprite sprite = projectilePrefab as Sprite;
            if (sprite != null)
            {
                previousButton.gameObject.transform.GetChild(0).gameObject.GetComponent<Image>().sprite = sprite;
                return;
            }

            previousButton.gameObject.transform.GetChild(0).gameObject.GetComponent<Image>().sprite = pt.GetThumbnail(projectilePrefab as GameObject);
        }

        public void SetPowerEnabled(bool enabled)
        {
            powerIcon.gameObject.transform.parent.gameObject.GetComponent<Image>().enabled = enabled;
            powerIcon.enabled = enabled;
        }

        //0 to 1
        public void SetPower(float power)
        {
            float pwr = Mathf.Clamp01(power);
            powerIcon.rectTransform.localScale = Vector3.one * pwr;

            // color: green → yellow → red
            powerIcon.color = pwr < 0.5f
                ? Color.Lerp(Color.green, Color.yellow, pwr * 2f)
                : Color.Lerp(Color.yellow, Color.red, (pwr - 0.5f) * 2f);
        }
    }
}
