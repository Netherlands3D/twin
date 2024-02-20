using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(Camera))]
    public class OpticalRaycaster : MonoBehaviour
    { 
        [SerializeField] private Camera depthCamera;
        float totalDepth = 0;
        private Texture2D samplerTexture;
        
        [Header("Events")]
        [SerializeField] public UnityEvent<Vector3> OnDepthSampled;

        void Start()
        {
            if(depthCamera.targetTexture == null)
            {
                Debug.Log("Depth camera has no target texture. Please assign a render texture to the depth camera.",this.gameObject);
                this.enabled = false;
                return;
            }

            //We will only render on demand using camera.Render()
            depthCamera.enabled = false; 

            //Create a red channel texture that we can sample depth from
            samplerTexture = new Texture2D(depthCamera.targetTexture.width, depthCamera.targetTexture.height, TextureFormat.R16, false);
        }

        private void OnDestroy() {
            Destroy(samplerTexture);
        }

        public Vector3 GetCameraDepthAtScreenPoint(Camera camera, Vector3 screenPoint)
        {
            //Align and rotate sampler camera to look at screenpoint
            depthCamera.transform.position = camera.transform.position;      
            depthCamera.transform.LookAt(camera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, camera.nearClipPlane)));

            return GetSamplerCameraDepth();
        }

        public Vector3 GetDepthFromWorldPoint(Vector3 worldPoint, Vector3 direction)
        {
            //Align depth camera 
            depthCamera.transform.SetPositionAndRotation(worldPoint, Quaternion.LookRotation(direction));

            return GetSamplerCameraDepth();
        }

        public Vector3 GetSamplerCameraDepth()
        {
            //Read pixels from the depth texture
            depthCamera.Render();
            RenderTexture.active = depthCamera.targetTexture;
            samplerTexture.ReadPixels(new Rect(0, 0, depthCamera.targetTexture.width, depthCamera.targetTexture.height), 0, 0);
            samplerTexture.Apply();
            RenderTexture.active = null;

            CalculateAverageDepth();

#if UNITY_EDITOR || !UNITY_WEBGL
            //WebGL/OpenGL2 raw depth value is inverted
            totalDepth = 1 - totalDepth;
#endif

            //Move far clip plane according to camera height to maintain precision
            depthCamera.farClipPlane = depthCamera.transform.position.y * 2.0f;

            //Use camera near and far to determine totalDepth value
            totalDepth = Mathf.Lerp(depthCamera.nearClipPlane, depthCamera.farClipPlane, totalDepth);

            var worldPoint = depthCamera.transform.position + depthCamera.transform.forward * totalDepth;
            OnDepthSampled.Invoke(worldPoint);

            return worldPoint;
        }

        /// <summary>
        /// Read all the pixels in the rendertexture, and use the average as our depth
        /// </summary>
        private void CalculateAverageDepth()
        {
            totalDepth = 0;
            for (int x = 0; x < samplerTexture.width; x++)
            {
                for (int y = 0; y < samplerTexture.height; y++)
                {
                    totalDepth += samplerTexture.GetPixel(x, y).r;
                }
            }
            totalDepth /= (samplerTexture.width * samplerTexture.height);
        }
    }
}
