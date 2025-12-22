using System;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D
{
    public class ProjectileSelected : MonoBehaviour
    {
        [SerializeField] private Image projectileIcon;
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
