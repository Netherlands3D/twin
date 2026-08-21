using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Netherlands3D.Twin.Samplers
{
    public class OpticalRaycaster : MonoBehaviour
    {
        [SerializeField] private Camera depthCamera;
        private Texture2D samplerTexture;
        private RenderTexture renderTexture;
        private const int defaultRaycastLayers = ~((1 << 2) + (1 << 12) + (1 << 13) + (1 << 14)); // all layers except IgnoreRaycast, Projected, PolygonMask, PolygonMaskInverted
        private const int MINIMUM_DEPTH_BUFFER_FORMAT = 16; //In the render graph API, the output Render Texture must have a depth buffer, this is the minimum value to keep the render texture light weight.
        
        void Start()
        {
            //We will only render on demand using camera.Render()
            depthCamera.enabled = false;
            
            //because of webgl we cannot create a rendertexture with the prefered format.
            //the following error will occur in webgl if done so:
            //RenderTexture.Create failed: format unsupported for random writes - RGBA32 SFloat (52).
            //weirdly enough creating a deptht5exture in project and passing it through a serializefield is ok on webgl
            //but we cannot do this since we need a pool and create a rendertexture for each request
            
            var graphicsFormat = SystemInfo.GetCompatibleFormat(GraphicsFormat.R32G32B32A32_SFloat, FormatUsage.Render);
            renderTexture = new RenderTexture(1, 1, MINIMUM_DEPTH_BUFFER_FORMAT, RenderTextureFormat.Depth)
            {
                graphicsFormat = graphicsFormat,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            renderTexture.Create();
            depthCamera.targetTexture = renderTexture;
            
            //Create a red channel texture that we can sample depth from
            samplerTexture = new Texture2D(depthCamera.targetTexture.width, depthCamera.targetTexture.height, TextureFormat.RGBAFloat, false);
        }

        private void OnDestroy()
        {
            Destroy(samplerTexture);
            depthCamera.targetTexture = null; 
            renderTexture.Release();
            Destroy(renderTexture);
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

        private void AlignDepthCameraFromPositionToDirection(Vector3 position, Vector3 direction)
        {
            //Align depth camera 
            depthCamera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
        }

        private void RenderDepthCamera()
        {
            //Read pixels from the depth texture
            depthCamera.Render();
            RenderTexture.active = depthCamera.targetTexture;
            samplerTexture.ReadPixels(new Rect(0, 0, depthCamera.targetTexture.width, depthCamera.targetTexture.height), 0, 0);
            RenderTexture.active = null;
        }
    }
}