using System;
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
        private Stack<MultiPointCallback> requestMultipointPool = new Stack<MultiPointCallback>();

        private const int maxRequests = 3;
        private const int defaultRaycastLayers = ~((1 << 2) + (1 << 12) + (1 << 13) + (1 << 14)); // all layers except IgnoreRaycast, Projected, PolygonMask, PolygonMaskInverted

        public void GetWorldPointAsync(Vector3 screenPoint, Action<Vector3, bool> callback, int cullingMask = defaultRaycastLayers)
        {
            if (activeRequests.Count > maxRequests)
            {
                callback.Invoke(Vector3.zero, false);
                return;
            }

            OpticalRequest opticalRequest = GetRequest();
            opticalRequest.SetCullingMask(cullingMask);
            opticalRequest.SetScreenPoint(screenPoint);
            opticalRequest.AlignWithMainCamera();
            opticalRequest.UpdateShaders();
            opticalRequest.SetResultCallback(callback);
            opticalRequest.framesActive = 0;
            activeRequests.Add(opticalRequest);
        }

        public void GetWorldPointsAsync(Vector3[] screenPoints, Action<Vector3[], bool> callback, int cullingMask = ~0)
        {
            if (activeRequests.Count > maxRequests)
            {
                callback.Invoke(null, false);
                return;
            }

            MultiPointCallback multipointCallback = GetMultipointCallback();
            multipointCallback.SetCallbackCompletion(callback);

            for (int i = 0; i < 4; i++)
            {
                OpticalRequest opticalRequest = GetRequest();
                opticalRequest.SetCullingMask(cullingMask);
                opticalRequest.SetScreenPoint(screenPoints[i]);
                opticalRequest.AlignWithMainCamera();
                opticalRequest.UpdateShaders();
                opticalRequest.SetResultCallback(multipointCallback.pointCallbacks[i]);
                opticalRequest.framesActive = 0;
                activeRequests.Add(opticalRequest);
            }
        }

        private void Update()
        {
            if (activeRequests.Count == 0) return;


            for (int i = activeRequests.Count - 1; i >= 0; i--)
            {
                activeRequests[i].framesActive++;
                if (activeRequests[i].framesActive > 1)
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

            try
            {
                var worldPosData = opticalRequest.request.GetData<Vector4>();
                if(worldPosData == null ||  worldPosData.Length == 0)
                {
                    opticalRequest.hasHit = false;
                    opticalRequest.resultCallback.Invoke(Vector3.zero, false);
                    Debug.LogError("readback has invalid data");
                    return;
                }
                float worldPosX = worldPosData[0].x;
                float worldPosY = worldPosData[0].y;
                float worldPosZ = worldPosData[0].z;
                Vector3 worldPos = new Vector3(worldPosX, worldPosY, worldPosZ);
                opticalRequest.hasHit = worldPosData[0].w > 0;
                opticalRequest.resultCallback.Invoke(worldPos, opticalRequest.hasHit);
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception in optical request: {e.StackTrace}");
            }
            finally
            {
                //we must always execute this to prevent crazy frame build ups
                PoolRequest(opticalRequest);
            }
        }

        private RenderTexture GetRenderTexture()
        {
            //because of webgl we cannot create a rendertexture with the prefered format.
            //the following error will occur in webgl if done so:
            //RenderTexture.Create failed: format unsupported for random writes - RGBA32 SFloat (52).
            //weirdly enough creating a depthtexture in project and passing it through a serializefield is ok on webgl
            //but we cannot do this since we need a pool and create a rendertexture for each request
            RenderTexture renderTexture = new RenderTexture(1, 1, 0, RenderTextureFormat.Depth);
            renderTexture.graphicsFormat = SystemInfo.GetCompatibleFormat(GraphicsFormat.R32G32B32A32_SFloat, FormatUsage.Render);
            renderTexture.depthStencilFormat = GraphicsFormat.None;
            renderTexture.Create();
            return renderTexture;
        }

        private OpticalRequest GetRequest()
        {
            OpticalRequest request = null;
            if (requestPool.Count > 0)
            {
                request = requestPool.Pop();
            }
            else
            {
                request = new OpticalRequest(depthToWorldMaterial, visualizationMaterial, GetRenderTexture(), depthCameraPrefab);
                request.depthCamera.transform.SetParent(gameObject.transform, false);
                request.SetCallback(RequestCallBackMapped(request));
            }

            request.depthCamera.enabled = true;
            return request;
        }

        private Action<AsyncGPUReadbackRequest> RequestCallBackMapped(OpticalRequest requester)
        {
            return w => RequestCallback(requester);
        }

        private void PoolRequest(OpticalRequest request)
        {
            if (request.depthCamera != null)
            {
                request.depthCamera.enabled = false;
                requestPool.Push(request);
            }
        }

        private MultiPointCallback GetMultipointCallback()
        {
            MultiPointCallback callback = null;
            if (requestMultipointPool.Count > 0)
            {
                callback = requestMultipointPool.Pop();
            }
            else
            {
                callback = new MultiPointCallback(() => { PoolMultipointCallback(callback); });
            }

            callback.Reset();
            return callback;
        }

        private void PoolMultipointCallback(MultiPointCallback callback)
        {
            requestMultipointPool.Push(callback);
        }

        //the following classes are private because they should only be used within optical raycaster
        private sealed class OpticalRequest
        {
            public Camera depthCamera;
            public Material depthMaterial;
            public Material positionMaterial;
            public RenderTexture renderTexture;
            public Vector3 screenPoint;
            public AsyncGPUReadbackRequest request;
            public Action<AsyncGPUReadbackRequest> callback;
            public Action<Vector3, bool> resultCallback;
            public Action onWaitFrameCallback;
            public int framesActive = 0;
            public int resultCount = 0;
            public bool hasHit = false;

            public OpticalRequest(Material depthMaterial, Material positionMaterial, RenderTexture rt, Camera prefab)
            {
                this.depthMaterial = new Material(depthMaterial);
                this.positionMaterial = new Material(positionMaterial);
                this.renderTexture = rt;
                this.depthCamera = Instantiate(prefab);
                depthCamera.clearFlags = CameraClearFlags.SolidColor;
                depthCamera.backgroundColor = Color.clear;
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

            public void SetResultCallback(Action<Vector3, bool> resultCallback)
            {
                this.resultCallback = resultCallback;
            }

            public void SetRequest(AsyncGPUReadbackRequest request)
            {
                this.request = request;
            }

            public void SetScreenPoint(Vector3 screenPoint)
            {
                this.screenPoint = screenPoint;
            }

            public void SetCullingMask(int mask)
            {
                depthCamera.cullingMask = mask;
            }

            public void AlignWithMainCamera()
            {
                depthCamera.transform.position = Camera.main.transform.position;
                if (Camera.main.orthographic)
                {
                    Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, Camera.main.nearClipPlane));
                    depthCamera.transform.position = worldPoint - Camera.main.transform.forward * 10f; //needing a temp offset position to simulate a depth offset, because ortho cameras ignore dpeth
                    depthCamera.transform.LookAt(worldPoint);
                }
                else
                {
                    Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, Camera.main.nearClipPlane));
                    depthCamera.transform.LookAt(worldPoint);
                }
            }

            public void UpdateShaders()
            {
                if (depthMaterial == null) return;

                depthMaterial.SetTexture("_CameraDepthTexture", renderTexture);
                depthMaterial.SetMatrix("_CameraInvProjection", depthCamera.projectionMatrix.inverse);
                positionMaterial.SetTexture("_WorldPositionTexture", renderTexture);
            }
        }

        //when getting a batch of points async we need to have a callback that can sync all points as one result
        private sealed class MultiPointCallback
        {
            public Action<Vector3, bool>[] pointCallbacks = new Action<Vector3, bool>[4];
            private int callbackCount = 0;
            private Vector3[] result = new Vector3[4];
            private Action<Vector3[], bool> callback;
            private Action onComplete;

            public MultiPointCallback(Action onComplete)
            {
                for (int i = 0; i < 4; i++)
                {
                    int index = i;
                    pointCallbacks[index] = (p, h) => InvokeCallback(index, p, h);
                }

                this.onComplete = onComplete;
            }

            public void InvokeCallback(int index, Vector3 point, bool hit)
            {
                callbackCount++;
                result[index] = point;
                if (callbackCount >= 4)
                    this.callback.Invoke(result, hit);
                onComplete.Invoke();
            }

            public void SetCallbackCompletion(Action<Vector3[], bool> callback)
            {
                this.callback = callback;
            }

            public void Reset()
            {
                callbackCount = 0;
            }
        }
    }
}