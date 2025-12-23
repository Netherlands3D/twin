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

        public void SetImage(GameObject projectilePrefab)
        {
            projectileIcon.sprite = pt.GetThumbnail(projectilePrefab);;
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
