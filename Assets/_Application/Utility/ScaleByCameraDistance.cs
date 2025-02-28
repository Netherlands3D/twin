using UnityEngine;

namespace Netherlands3D.Twin.Utility
{
    public class ScaleByCameraDistance : MonoBehaviour
    {
        [SerializeField] private float scaleMultiplier = 1.0f;
        private Camera targetCamera;

        void Start()
        {
            targetCamera = Camera.main;
        }
        void Update()
        {
            this.transform.localScale = Vector3.one * Vector3.Distance(targetCamera.transform.position,transform.position) * scaleMultiplier;
        }
    }
}
