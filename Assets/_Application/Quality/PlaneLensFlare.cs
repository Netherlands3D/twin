using System;
using Netherlands3D.Services;
using Netherlands3D.Twin.Cameras;
using UnityEngine;

namespace Netherlands3D.Twin.Quality
{
    public class PlaneLensFlare : MonoBehaviour
    {
        [SerializeField][Tooltip("If not set, first Light will be used as Sun")] private Transform directionalLightSunTransform;
        [SerializeField] private Transform sunHalo;
        [SerializeField] private float haloScale = 1.0f;
        [SerializeField] private float offset = 0.01f;
        
        private Camera mainCamera;

        private void Awake() {
            if(!directionalLightSunTransform)
                directionalLightSunTransform = FindObjectOfType<Light>().transform;
        }

        private void Start()
        {
            mainCamera = ServiceLocator.GetService<CameraService>().ActiveCamera;
        }

        void Update()
        {
            if(!mainCamera.isActiveAndEnabled) return;

            this.transform.rotation = directionalLightSunTransform.rotation;
            this.transform.position = mainCamera.transform.position;

            sunHalo.transform.localPosition = -Vector3.forward * (mainCamera.farClipPlane + (mainCamera.farClipPlane*offset));
            sunHalo.transform.localScale = Vector3.one * mainCamera.farClipPlane * haloScale;
        }
    }
}