using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
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
            public Vector3[] screenPoints;
            public Matrix4x4[] screenPointMatrices;
            public AsyncGPUReadbackRequest request;
            public Action<AsyncGPUReadbackRequest> callback;
            public Action<Vector3[]> resultCallback;
            public Action onWaitFrameCallback;
            public int framesActive = 0;

            public OpticalRequest(Material depthMaterial, Material positionMaterial, RenderTexture rt, Camera prefab)
            {
                this.depthMaterial = new Material(depthMaterial);
                this.positionMaterial = new Material(positionMaterial);
                this.renderTexture = rt;
                this.depthCamera = Instantiate(prefab);
                screenPointMatrices = new Matrix4x4[rt.width * rt.height];
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

            public void SetResultCallback(Action<Vector3[]> resultCallback)
            {
                this.resultCallback = resultCallback;
            }

            public void SetRequest(AsyncGPUReadbackRequest request)
            {
                this.request = request; 
            }

            public void SetScreenPoints(Vector3[] screenPoints)
            {
                this.screenPoints = screenPoints;
            }

            public void AlignWithMainCamera()
            {
                depthCamera.transform.position = Camera.main.transform.position;
                for (int i = 0; i < screenPoints.Length; i++)
                {
                    Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(screenPoints[i].x, screenPoints[i].y, Camera.main.nearClipPlane));
                    depthCamera.transform.LookAt(worldPoint);
                    screenPointMatrices[i] = depthCamera.projectionMatrix.inverse;
                }                
            }

            public void UpdateShaders()
            {
                depthMaterial.SetTexture("_CameraDepthTexture", renderTexture);
                depthMaterial.SetMatrixArray("_CameraInvProjection", screenPointMatrices);
                positionMaterial.SetTexture("_WorldPositionTexture", renderTexture);
            }
        }

        public void GetWorldPointAsync(Vector3 screenPoint, Action<Vector3> callback)
        {
            GetWorldPointsAsync(new Vector3[1] { screenPoint }, result => callback(result[0]));
        }

        public void GetWorldPointsAsync(Vector3[] screenPoints, Action<Vector3[]> callback)
        {
            OpticalRequest opticalRequest = GetRequest();
            opticalRequest.SetScreenPoints(screenPoints);
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
            //var worldPosData = opticalRequest.request.GetData<Vector4>();
            //float worldPosX = worldPosData[0].x;
            //float worldPosY = worldPosData[0].y;
            //float worldPosZ = worldPosData[0].z;
            //Vector3 worldPos = new Vector3(worldPosX, worldPosY, worldPosZ);

            NativeArray<Vector4> worldPosData = opticalRequest.request.GetData<Vector4>();

            // Extract four world positions from the 2x2 texture
            Vector3 bottomLeft = new Vector3(worldPosData[0].x, worldPosData[0].y, worldPosData[0].z);
            Vector3 bottomRight = new Vector3(worldPosData[1].x, worldPosData[1].y, worldPosData[1].z);
            Vector3 topLeft = new Vector3(worldPosData[2].x, worldPosData[2].y, worldPosData[2].z);
            Vector3 topRight = new Vector3(worldPosData[3].x, worldPosData[3].y, worldPosData[3].z);
            Vector3[] results = new Vector3[4] { bottomLeft, bottomRight, topLeft, topRight };

            opticalRequest.resultCallback.Invoke(results);
            PoolRequest(opticalRequest);            
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
            //because of webgl we cannot create a rendertexture with the prefered format.
            //the following error will occur in webgl if done so:
            //RenderTexture.Create failed: format unsupported for random writes - RGBA32 SFloat (52).
            //weirdly enough creating a depthtexture in project and passing it through a serializefield is ok on webgl
            //but we cannot do this since we need a pool and create a rendertexture for each request
            RenderTexture renderTexture = new RenderTexture(2, 2, 0, RenderTextureFormat.Depth);
            renderTexture.graphicsFormat = SystemInfo.GetCompatibleFormat(GraphicsFormat.R32G32B32A32_SFloat, FormatUsage.Render);
            renderTexture.depthStencilFormat = GraphicsFormat.None;
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