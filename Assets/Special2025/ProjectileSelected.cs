using System;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D
{
    public class ProjectileSelected : MonoBehaviour
    {
        public Button next => nextButton;
        public Button previous => previousButton;
        
        [SerializeField] private Image projectileIcon;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button previousButton;
        
        private PrefabThumbnail pt;

        private void Awake()
        {
            pt = GetComponent<PrefabThumbnail>();
        }

        public void SetImage(GameObject projectilePrefab)
        {
            projectileIcon.sprite = pt.GetThumbnail(projectilePrefab);;
        }
    }
}
