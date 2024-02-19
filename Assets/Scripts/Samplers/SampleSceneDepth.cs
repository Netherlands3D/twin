using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(Camera))]
    public class SampleSceneDepth : MonoBehaviour
    { 
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera depthCamera;

        private Matrix4x4 projectionMatrix;

        [Tooltip("Visual feedback in editor")]
        [SerializeField] private RenderTexture depthTexture;

        void Start()
        {
            if(!mainCamera)
            {
                mainCamera = Camera.main;
            }

            //Use same fov settings from as main camera
            depthCamera.farClipPlane = mainCamera.farClipPlane;
            depthCamera.nearClipPlane = mainCamera.nearClipPlane;
            depthCamera.fieldOfView = mainCamera.fieldOfView;
            depthCamera.orthographic = mainCamera.orthographic;
            depthCamera.orthographicSize = mainCamera.orthographicSize;
            depthCamera.aspect = mainCamera.aspect;
        }

        public void GetDepthFromCamera(Vector3 screenPosition)
        {
            //Shift camera matrix so the rendered area is centered on the screenposition
            depthCamera.projectionMatrix = mainCamera.projectionMatrix;
            depthCamera.transform.SetPositionAndRotation(mainCamera.transform.position, mainCamera.transform.rotation);
        }
    }
}
