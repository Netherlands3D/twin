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

        private Vector3 screenPoint;
        float totalDepth = 0;
        private Vector3 worldPoint;
        private Texture2D samplerTexture;

        private Transform testTarget;

        [SerializeField] private float padding = 1.0f;

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
            depthCamera.orthographic = false;
            depthCamera.aspect = 1.0f;

            //We will only render on demand using camera.Render()
            depthCamera.enabled = false; 

            samplerTexture = new Texture2D(depthCamera.targetTexture.width, depthCamera.targetTexture.height, TextureFormat.RGB24, false);

            testTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            testTarget.transform.parent = depthCamera.transform;
        }

        private void Update()
        {
            if(Input.GetMouseButton(0))
            GetDepthFromCamera(Input.mousePosition);
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

            //Shift camera matrix so the rendered area is centered on the screenposition
            depthCamera.transform.SetPositionAndRotation(mainCamera.transform.position, mainCamera.transform.rotation);        

            //Look at the screen position with our orto sampler camera
            depthCamera.transform.LookAt(mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, mainCamera.nearClipPlane)));

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

            //Use camera near and far to determine totalDepth value
            totalDepth = Mathf.Lerp(depthCamera.nearClipPlane, depthCamera.farClipPlane, totalDepth / (samplerTexture.width * samplerTexture.height));
           
            var worldPoint = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, totalDepth - depthCamera.nearClipPlane));
            testTarget.position = worldPoint;

            Debug.Log($"Depth at screen position {screenPosition} is {totalDepth} units away from camera");
        }
    }
}
