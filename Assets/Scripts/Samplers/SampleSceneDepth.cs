using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

        private Vector3 screenPoint;
        float totalDepth = 0;
        private Vector3 worldPoint;
        private Texture2D samplerTexture;

        private Transform testTarget;

        [SerializeField] private float padding = 1.0f;

        [Header("Events")]
        [SerializeField] private UnityEvent<Vector3> OnDepthSampled;

        void Start()
        {
            if(!mainCamera)
            {
                mainCamera = Camera.main;
            }

            if(depthCamera.targetTexture == null)
            {
                Debug.Log("Depth camera has no target texture. Please assign a render texture to the depth camera.");
                this.enabled = false;
                return;
            }

            //Use same fov settings from as main camera
            depthCamera.farClipPlane = 1000.0f;
            depthCamera.nearClipPlane = 0.3f;
            depthCamera.fieldOfView = 1.0f;
            depthCamera.orthographic = true;
            depthCamera.aspect = 1.0f;

            //We will only render on demand using camera.Render()
            depthCamera.enabled = false; 

            samplerTexture = new Texture2D(depthCamera.targetTexture.width, depthCamera.targetTexture.height, TextureFormat.R16, false);
        }

        private void Update()
        {
            screenPoint = Input.mousePosition;

            //Rotate sampler camera to match main camera, and lookat the screen position
            depthCamera.transform.SetPositionAndRotation(mainCamera.transform.position, mainCamera.transform.rotation);        
            depthCamera.transform.LookAt(mainCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, mainCamera.nearClipPlane)));

            GetDepthFromCamera(screenPoint);
        }

        private void OnDrawGizmos() {
            if(testTarget)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(mainCamera.transform.position, testTarget.position);
            }
        }

        public void GetDepthFromCamera(Vector3 screenPosition)
        {
            screenPoint = screenPosition;

            //Read pixels from the depth texture
            depthCamera.Render();
            RenderTexture.active = depthCamera.targetTexture;
            samplerTexture.ReadPixels(new Rect(0, 0, depthCamera.targetTexture.width, depthCamera.targetTexture.height), 0, 0);
            samplerTexture.Apply();
            RenderTexture.active = null;

            //Read all pixels their red value, and determine average depth
            totalDepth = 0;
            for (int x = 0; x < samplerTexture.width; x++)
            {
                for (int y = 0; y < samplerTexture.height; y++)
                {
                    totalDepth += samplerTexture.GetPixel(x, y).r;
                }
            }  
            totalDepth /= (samplerTexture.width * samplerTexture.height);

            //Move far clip plane according to camera height to maintain a consistent depth value
            depthCamera.farClipPlane = depthCamera.transform.position.y * 3.0f;

            //Use camera near and far to determine totalDepth value
            totalDepth = Mathf.Lerp(depthCamera.farClipPlane,depthCamera.nearClipPlane, totalDepth);
           
            var worldPoint = depthCamera.transform.position + depthCamera.transform.forward * totalDepth;

            OnDepthSampled.Invoke(worldPoint);
            Debug.Log($"Depth at screen position {screenPoint} is {totalDepth} units away from camera");
        }
    }
}
