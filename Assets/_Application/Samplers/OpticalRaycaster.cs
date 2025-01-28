using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin.Samplers
{
    public class OpticalRaycaster : MonoBehaviour
    {
        public Camera depthCameraPrefab; // Camera to capture depth information
        public Material depthToWorldMaterial;
        public Material visualizationMaterial; // Material to visualize world position

        private List<OpticalRequest> requestPool = new List<OpticalRequest>();


        private class OpticalRequest
        {
            public Camera depthCamera;
            public Material depthMaterial;
            public Material positionMaterial;
            public RenderTexture renderTexture;
            public Vector3 screenPoint;
            public AsyncGPUReadbackRequest request;

            public OpticalRequest(Material depthMaterial, Material positionMaterial, RenderTexture rt, Camera prefab)
            {
                this.depthMaterial = new Material(depthMaterial);
                this.positionMaterial = new Material(positionMaterial);
                this.renderTexture = rt;
                this.depthCamera = GameObject.Instantiate(prefab);
                depthCamera.clearFlags = CameraClearFlags.SolidColor;
                depthCamera.backgroundColor = Color.black;
                depthCamera.depthTextureMode = DepthTextureMode.Depth;
                depthCamera.targetTexture = rt;
                depthCamera.forceIntoRenderTexture = true;
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

            opticalRequest.depthCamera.transform.SetParent(gameObject.transform, false);

            // Trigger async readback
            AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(opticalRequest.renderTexture, 0, w =>
            {
                if (w.hasError)
                {
                    Debug.LogError("GPU readback failed!");
                    PoolRequest(opticalRequest);
                    return;
                }
                // The result is now available in request.GetData<Vector4>()
                var worldPosData = w.GetData<Vector4>();

                // Example: Extracting the world position from the readback data
                float worldPosX = worldPosData[0].x;
                float worldPosY = worldPosData[0].y;
                float worldPosZ = worldPosData[0].z;
                Vector3 worldPos = new Vector3(worldPosX, worldPosY, worldPosZ);
                callback.Invoke(worldPos);
                Debug.Log(worldPos);
                PoolRequest(opticalRequest);
            });
            opticalRequest.SetRequest(request);
        }

        private IEnumerator WaitForFrame(Action onEnd)
        {
            yield return new WaitForEndOfFrame();
            onEnd?.Invoke();
        }

        private OpticalRequest GetRequest()
        {
            OpticalRequest request = null;
            if(requestPool.Count > 0)
            {
                request = requestPool[0];
                requestPool.RemoveAt(0);
            }
            else
            {
                request = new OpticalRequest(depthToWorldMaterial, visualizationMaterial, GetRenderTexture(), Instantiate(depthCameraPrefab));
            }
            return request;
        }


        private RenderTexture GetRenderTexture()
        {
            RenderTexture renderTexture = new RenderTexture(1, 1, 0, RenderTextureFormat.Depth);
            renderTexture.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
            renderTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;
            //renderTexture.enableRandomWrite = true; // Allow GPU writes
            renderTexture.Create();
            return renderTexture;
        }

        private void PoolRequest(OpticalRequest request)
        {
            requestPool.Add(request);
        }
    }
}