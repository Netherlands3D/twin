using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Samplers
{
    [RequireComponent(typeof(Camera))]
    public class OpticalRaycaster : MonoBehaviour
    {
        [SerializeField] private Camera depthCamera;
        public RenderTexture worldPosRenderTexture;
        float totalDepth = 0;
        private Texture2D samplerTexture;
        public Material depthMaterial;

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

            samplerTexture = new Texture2D(depthCamera.targetTexture.width, depthCamera.targetTexture.height, TextureFormat.RGBAFloat, false);

            depthMaterial = new Material(Shader.Find("Custom/WorldPositionFromDepth"));
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
        public Vector3 GetWorldPointAtCameraScreenPoint(Camera camera, Vector3 screenPoint)
        {
            AlignDepthCameraToScreenPoint(camera, screenPoint);
            RenderDepthCamera();

            return GetDepthCameraWorldPoint();
        }

        public Vector3 GetWorldPointFromPosition(Vector3 position, Vector3 direction)
        {
            AlignDepthCameraFromPositionToDirection(position, direction);
            RenderDepthCamera();

            return GetDepthCameraWorldPoint();
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

        public Vector3 GetDepthCameraWorldPoint()
        {
            var worldPoint = ReadWorldPositionFromPixel();
            OnDepthSampled.Invoke(worldPoint);

            return worldPoint;
        }

        public void RenderDepthCamera()
        {
            ////Read pixels from the depth texture
            depthCamera.Render();
            RenderTexture.active = depthCamera.targetTexture;
            samplerTexture.ReadPixels(new Rect(0, 0, depthCamera.targetTexture.width, depthCamera.targetTexture.height), 0, 0);
            samplerTexture.Apply();
            RenderTexture.active = null;

            //ReadWorldPositionFromTexture();
        }

        private void RenderDepthCameraWithShader()
        {
            // Set camera inverse view-projection matrix for the shader
            Matrix4x4 invViewProjection = Camera.main.projectionMatrix.inverse * Camera.main.worldToCameraMatrix.inverse;
            depthMaterial.SetMatrix("_CameraInvViewProjection", invViewProjection);

            // Set near and far clip planes for depth calculations
            depthMaterial.SetVector("_MyScreenParams", new Vector4(Camera.main.nearClipPlane, Camera.main.farClipPlane, 0, 0));

            // Set the RenderTexture as the target for depthCamera's output
            depthCamera.targetTexture = worldPosRenderTexture;

            // Use Graphics.Blit to apply the material and render to the RenderTexture
            Graphics.Blit(null, worldPosRenderTexture, depthMaterial);

            // Reset the target texture (optional, if needed)
            depthCamera.targetTexture = null;
        }


        private Vector3 ReadWorldPositionFromPixel()
        {
            var worldPosition = samplerTexture.GetPixel(0, 0);

            return new Vector3(
                worldPosition.r,
                worldPosition.g,
                worldPosition.b
            );
        }

        private Vector3 ReadWorldPositionFromTexture()
        {
            // Sample the pixel value from the RenderTexture
            RenderTexture.active = depthCamera.targetTexture;
            samplerTexture.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
            samplerTexture.Apply();
            RenderTexture.active = null;

            Color worldPositionColor = samplerTexture.GetPixel(0, 0);
            return new Vector3(worldPositionColor.r, worldPositionColor.g, worldPositionColor.b);
        }
    }
}