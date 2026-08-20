using UnityEngine;

namespace Netherlands3D.Twin.Samplers
{
    public class OpticalRaycaster : MonoBehaviour
    {
        [SerializeField] private Camera depthCamera;
        private Texture2D samplerTexture;
        private const int defaultRaycastLayers = ~((1 << 2) + (1 << 12) + (1 << 13) + (1 << 14)); // all layers except IgnoreRaycast, Projected, PolygonMask, PolygonMaskInverted

        void Start()
        {
            //We will only render on demand using camera.Render()
            depthCamera.enabled = false;
            
            //Create a red channel texture that we can sample depth from
            samplerTexture = new Texture2D(depthCamera.targetTexture.width, depthCamera.targetTexture.height, TextureFormat.RGBAFloat, false);
        }

        private void OnDestroy()
        {
            Destroy(samplerTexture);
        }
        
        /// <summary>
        /// Get's the worldPoint synchronously.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="screenPoint"></param>
        /// <param name="worldPosition"></param>
        /// <param name="cullingMask"></param>
        /// <returns></returns>
        public bool TryGetWorldPoint(Camera camera, Vector2 screenPoint, out Vector3 worldPosition, int cullingMask = defaultRaycastLayers)
        {
            AlignWithCamera(camera, screenPoint);
            
            depthCamera.cullingMask = cullingMask;
            RenderDepthCamera();
            
            var pixel = samplerTexture.GetPixel(0, 0);
            worldPosition = new Vector3(pixel.r, pixel.g, pixel.b);

            return pixel.a > 0;
        }
        
        public bool TryGetWorldPointFromDirection(Vector3 origin, Vector3 direction, out Vector3 hitPosition, int cullingMask = defaultRaycastLayers)
        {
            AlignDepthCameraFromPositionToDirection(origin, direction);

            depthCamera.cullingMask = cullingMask;
            RenderDepthCamera();
            
            var pixel = samplerTexture.GetPixel(0, 0);
            hitPosition = new Vector3(pixel.r, pixel.g, pixel.b);

            return pixel.a > 0;
        }

        private void AlignWithCamera(Camera camera, Vector3 screenPoint)
        {
            if (camera == null) camera = Camera.main;

            depthCamera.transform.position = camera.transform.position;
            if (camera.orthographic)
            {
                Vector3 worldPoint = camera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, camera.nearClipPlane)); 
                depthCamera.transform.position = worldPoint - camera.transform.forward * 10f; //needing a temp offset position to simulate a depth offset, because ortho cameras ignore dpeth
                depthCamera.transform.LookAt(worldPoint);
            }
            else
            {
                Vector3 worldPoint = camera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, camera.nearClipPlane));
                depthCamera.transform.LookAt(worldPoint);
            }
        }

        public void AlignDepthCameraFromPositionToDirection(Vector3 position, Vector3 direction)
        {
            //Align depth camera 
            depthCamera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
        }

        public void RenderDepthCamera()
        {
            //Read pixels from the depth texture
            depthCamera.Render();
            RenderTexture.active = depthCamera.targetTexture;
            samplerTexture.ReadPixels(new Rect(0, 0, depthCamera.targetTexture.width, depthCamera.targetTexture.height), 0, 0);
            RenderTexture.active = null;
        }
    }
}