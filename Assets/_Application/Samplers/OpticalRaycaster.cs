using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin.Samplers
{
    public class OpticalRaycaster : MonoBehaviour
    {
        public Camera depthCameraPrefab; 
        public Material depthToWorldMaterial; //capture depth data shader
        public Material visualizationMaterial; //convert to temp position data

        private Stack<OpticalRequest> requestPool = new Stack<OpticalRequest>();
        private List<OpticalRequest> activeRequests = new List<OpticalRequest>();

        private class OpticalRequest
        {
            public Camera depthCamera;
            public Material depthMaterial;
            public Material positionMaterial;
            public RenderTexture renderTexture;
            public Vector3 screenPoint;
            public AsyncGPUReadbackRequest request;
            public Action<AsyncGPUReadbackRequest> callback;
            public Action<Vector3> resultCallback;
            public Action onWaitFrameCallback;
            public int framesActive = 0;

            public OpticalRequest(Material depthMaterial, Material positionMaterial, RenderTexture rt, Camera prefab)
            {
                this.depthMaterial = new Material(depthMaterial);
                this.positionMaterial = new Material(positionMaterial);
                this.renderTexture = rt;
                this.depthCamera = Instantiate(prefab);
                depthCamera.clearFlags = CameraClearFlags.SolidColor;
                depthCamera.backgroundColor = Color.black;
                depthCamera.depthTextureMode = DepthTextureMode.Depth;
                depthCamera.targetTexture = rt;
                depthCamera.forceIntoRenderTexture = true;
                onWaitFrameCallback = () =>
                {
                    AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(renderTexture, 0, callback);
                    SetRequest(request);
                };

            }        
            
            public void SetCallback(Action<AsyncGPUReadbackRequest> callback)
            {
                this.callback = callback;
            }

            public void SetResultCallback(Action<Vector3> resultCallback)
            {
                this.resultCallback = resultCallback;
            }

            public void SetWaitFrameCallback(Action callback)
            {
                onWaitFrameCallback = callback;
            }

            public void SetRequest(AsyncGPUReadbackRequest request)
            {
                this.request = request; 
            }

            public void SetScreenPoint(Vector3 screenPoint)
            {
                this.screenPoint = screenPoint;
            }

            public void AlignWithMainCamera()
            {
                depthCamera.transform.position = Camera.main.transform.position;
                depthCamera.transform.LookAt(Camera.main.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, Camera.main.nearClipPlane)));
            }

            public void UpdateShaders()
            {
                depthMaterial.SetTexture("_CameraDepthTexture", renderTexture);
                depthMaterial.SetMatrix("_CameraInvProjection", depthCamera.projectionMatrix.inverse);
                positionMaterial.SetTexture("_WorldPositionTexture", renderTexture);
            }
        }

        public void GetWorldPointAsync(Vector3 screenPoint, Action<Vector3> callback)
        {
            OpticalRequest opticalRequest = GetRequest();
            opticalRequest.SetScreenPoint(screenPoint);
            opticalRequest.AlignWithMainCamera();
            opticalRequest.UpdateShaders();           
            opticalRequest.SetResultCallback(callback);
            opticalRequest.framesActive = 0;
            activeRequests.Add(opticalRequest);
        }

        private void Update()
        {
            if (activeRequests.Count == 0) return;

            for(int i = activeRequests.Count - 1; i >= 0; i--) 
            {
                activeRequests[i].framesActive++;
                if(activeRequests[i].framesActive > 1)
                {
                    //we need to wait a frame to be sure the depth camera is rendered (camera.Render is very heavy to manualy call)
                    activeRequests[i].onWaitFrameCallback();
                    activeRequests.RemoveAt(i);
                }
            }
        }

        private void RequestCallback(OpticalRequest opticalRequest)
        {
            if (opticalRequest.request.hasError)
            {
                Debug.LogError("GPU readback failed!");
                PoolRequest(opticalRequest);
                return;
            }
            var worldPosData = opticalRequest.request.GetData<Vector4>();
            float worldPosX = worldPosData[0].x;
            float worldPosY = worldPosData[0].y;
            float worldPosZ = worldPosData[0].z;
            Vector3 worldPos = new Vector3(worldPosX, worldPosY, worldPosZ);
            opticalRequest.resultCallback.Invoke(worldPos);
            PoolRequest(opticalRequest);            
        }

        private WaitForEndOfFrame wfs = new WaitForEndOfFrame();
        private IEnumerator WaitForFrame(Action onEnd)
        {
            yield return wfs;
            onEnd?.Invoke();
        }

        private OpticalRequest GetRequest()
        {
            OpticalRequest request = null;
            if(requestPool.Count > 0)
            {
                request = requestPool.Pop();
            }
            else
            {
                request = new OpticalRequest(depthToWorldMaterial, visualizationMaterial, GetRenderTexture(), depthCameraPrefab);
                request.depthCamera.transform.SetParent(gameObject.transform, false);
                request.SetCallback(w => RequestCallback(request));
            }
            request.depthCamera.enabled = true;
            return request;
        }

        private RenderTexture GetRenderTexture()
        {
            RenderTexture renderTexture = new RenderTexture(1, 1, 0, RenderTextureFormat.Depth);
            //renderTexture.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat;
            renderTexture.graphicsFormat = SystemInfo.GetCompatibleFormat(GraphicsFormat.R32G32B32A32_SFloat, FormatUsage.Render);
            renderTexture.depthStencilFormat = GraphicsFormat.None;
            //renderTexture.enableRandomWrite = true; // Allow GPU writes, check on webgl?
            renderTexture.Create();
            return renderTexture;
        }

        private void PoolRequest(OpticalRequest request)
        {
            request.depthCamera.enabled = false;
            requestPool.Push(request);
        }
    }
}