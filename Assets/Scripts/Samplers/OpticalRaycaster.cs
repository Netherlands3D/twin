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

        [Header("Events")] [SerializeField] public UnityEvent<Vector3> OnDepthSampled;
        
        void Start()
        {
            if (depthCamera.targetTexture == null)
            {
                Debug.Log("Depth camera has no target texture. Please assign a render texture to the depth camera.", this.gameObject);
                this.enabled = false;
                return;
            }

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
            //Read pixels from the depth texture
            depthCamera.Render();
            RenderTexture.active = depthCamera.targetTexture;
            samplerTexture.ReadPixels(new Rect(0, 0, depthCamera.targetTexture.width, depthCamera.targetTexture.height), 0, 0);
            samplerTexture.Apply();
            RenderTexture.active = null;
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
    }
}