using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin.Samplers
{
    public class OpticalRaycaster : MonoBehaviour
    {
        [SerializeField] private Camera depthCamera;

        [Header("Events")] [SerializeField] public UnityEvent<Vector3> OnDepthSampled;

        private List<RenderTexture> rtPool = new List<RenderTexture>();
        private Dictionary<Vector3, GPURequest> activeRequests = new();


        public struct GPURequest
        {
            public AsyncGPUReadbackRequest request;
            public List<Action<Vector3>> callbacks;
            public RenderTexture texture;

            public GPURequest(AsyncGPUReadbackRequest request, RenderTexture texture, Action<Vector3> firstCallback)
            {
                this.request = request;
                this.texture = texture; 
                callbacks = new List<Action<Vector3>>();
                callbacks.Add(firstCallback);
            }
        }

        void Start()
        {
            depthCamera.depthTextureMode = DepthTextureMode.Depth;
        }

        private void OnDestroy()
        {
            //release all rendertextures
            foreach (RenderTexture rt in rtPool)
            {
                rt.Release();
            }
        }

        /// <summary>
        /// Only use this method if it is used continiously in Update.
        /// If one sample is needed, use AlignDepthCameraToScreenPoint, and GetSamplerCameraWorldPoint
        /// in a Coroutine with a WaitForEndOfFrame between every step.
        /// </summary>
        /// <returns></returns>
        public void GetWorldPointAsync(Vector3 screenPoint, Action<Vector3> result)
        {
            if (activeRequests.ContainsKey(screenPoint))
            {
                activeRequests[screenPoint].callbacks.Add(result);
                return;
            }

            RenderTexture rt = GetRenderTexture();
            depthCamera.targetTexture = rt;

            //Align depthcamera to the main camera
            depthCamera.transform.position = Camera.main.transform.position;
            depthCamera.transform.LookAt(Camera.main.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, Camera.main.nearClipPlane)));
            depthCamera.Render();
            AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(rt, 0, (req) =>
            {
                if (req.hasError)
                {
                    Debug.LogError("Error in GPU readback");
                    CompleteRequest(Vector3.zero, screenPoint, rt);
                    return;
                }              
                var data = req.GetData<Color>();
                Color color = data[0];
                Vector3 worldPos = new Vector3(color.r, color.g, color.b);
                CompleteRequest(worldPos, screenPoint, rt);
            });
            activeRequests.Add(screenPoint, new GPURequest(request, rt, result));
        }

        private void CompleteRequest(Vector3 worldPosition, Vector3 screenPoint, RenderTexture rt)
        {
            foreach (var callback in activeRequests[screenPoint].callbacks)
                callback.Invoke(worldPosition);
            activeRequests.Remove(screenPoint);
            rtPool.Add(rt);
        }

        private RenderTexture GetRenderTexture()
        {
            RenderTexture renderTexture = null;
            if (rtPool.Count > 0)
            {
                renderTexture = rtPool[0];
                rtPool.RemoveAt(0);
            }
            else
            {
                renderTexture = new RenderTexture(1, 1, 0, RenderTextureFormat.Depth);
                renderTexture.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
                renderTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;
                //renderTexture.enableRandomWrite = true; // Allow GPU writes
                renderTexture.Create();
            }            
            return renderTexture;
        }
    }
}