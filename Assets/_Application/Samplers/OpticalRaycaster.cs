using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin.Samplers
{
    [RequireComponent(typeof(Camera))]
    public class OpticalRaycaster : MonoBehaviour
    {
        [SerializeField] private Camera depthCamera;
        public RenderTexture worldPosRenderTexture;
        float totalDepth = 0;
        private Texture2D samplerTexture;

        [Header("Events")] [SerializeField] public UnityEvent<Vector3> OnDepthSampled;
        
        void Start()
        {
            if (depthCamera.targetTexture == null)
            {
                Debug.Log("Depth camera has no target texture. Please assign a render texture to the depth camera.", this.gameObject);
                this.enabled = false;
                return;
            }

            worldPosRenderTexture = new RenderTexture(1, 1, 0, RenderTextureFormat.Depth);
            worldPosRenderTexture.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
            worldPosRenderTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;
            worldPosRenderTexture.enableRandomWrite = true; // Allow GPU writes
            worldPosRenderTexture.Create();

            depthCamera.targetTexture = worldPosRenderTexture;

            samplerTexture = new Texture2D(worldPosRenderTexture.width, worldPosRenderTexture.height, TextureFormat.RGBAFloat, false);
        }

        private void OnDestroy()
        {
            Destroy(samplerTexture);
            worldPosRenderTexture.Release();
        }

        /// <summary>
        /// Only use this method if it is used continiously in Update.
        /// If one sample is needed, use AlignDepthCameraToScreenPoint, and GetSamplerCameraWorldPoint
        /// in a Coroutine with a WaitForEndOfFrame between every step.
        /// </summary>
        /// <returns></returns>
        public void GetWorldPointAtCameraScreenPoint(Camera camera, Vector3 screenPoint, Action<Vector3> result)
        {
            AlignDepthCameraToScreenPoint(camera, screenPoint);
            GetWorldPoint(result);
        }


        public void AlignDepthCameraToScreenPoint(Camera camera, Vector3 screenPoint)
        {
            //Align and rotate sampler camera to look at screenpoint
            depthCamera.transform.position = camera.transform.position;
            depthCamera.transform.LookAt(camera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, camera.nearClipPlane)));
        }

        public void AlignDepthCameraFromPositionToDirection(Vector3 position, Vector3 direction)
        {
            //Align depth camera 
            depthCamera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
        }

        public void GetWorldPoint(Action<Vector3> gpuResult)
        {
            AsyncGPUReadback.Request(worldPosRenderTexture, 0, (request) =>
            {
                if (request.hasError)
                {
                    Debug.LogError("Error in GPU readback");
                    return;
                }

                // Get the raw byte data from the readback request
                var data = request.GetData<Color>();             
                Color color = data[0];
                Vector3 worldPos = new Vector3(color.r, color.g, color.b);
                gpuResult.Invoke(worldPos);
            });

            //RenderTexture.active = worldPosRenderTexture;
            //samplerTexture.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
            //RenderTexture.active = null;

            //Color sampledColor = samplerTexture.GetPixel(0, 0);

            //// De-normalize color to world position
            //return new Vector3(
            //    sampledColor.r,
            //    sampledColor.g,
            //    sampledColor.b
            //);
        }
    }
}