using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ComparisonCamera : MonoBehaviour
    {
        public Camera mainCamera;
        private Camera currentCamera;

        [Range(0f, 1f)]
        public float overlayX = 0.5f;
        [Range(0f, 1f)]
        public float overlayY = 0f;

        private void Awake()
        {
            currentCamera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            currentCamera.depth = mainCamera.depth + 1;
            currentCamera.transform.SetParent(mainCamera.transform);
            
            currentCamera.clearFlags = mainCamera.clearFlags;
            currentCamera.backgroundColor = mainCamera.backgroundColor;

            // Match other relevant settings
            currentCamera.cullingMask = mainCamera.cullingMask;
            currentCamera.orthographic = mainCamera.orthographic;
            currentCamera.orthographicSize = mainCamera.orthographicSize;
            
            currentCamera.fieldOfView = mainCamera.fieldOfView;

            // Match near and far clipping planes
            currentCamera.nearClipPlane = mainCamera.nearClipPlane;
            currentCamera.farClipPlane = mainCamera.farClipPlane;
        }

        private void Update()
        {
            currentCamera.rect = new Rect(overlayX, overlayY, 1f, 1f);

            AdjustProjectionMatrix();
        }

        void AdjustProjectionMatrix()
        {
            Matrix4x4 originalProjection = mainCamera.projectionMatrix;

            float verticalShift = currentCamera.rect.y * 2f - 1f; // Convert from 0-1 to -1 to 1 range

            // Calculate the scaling factors to compensate for the viewport rect
            float viewportWidth = currentCamera.rect.width - currentCamera.rect.x;
            float viewportHeight = currentCamera.rect.height - currentCamera.rect.y;
            float aspectRatio = viewportWidth / viewportHeight;
           
            Matrix4x4 modifiedProjection = originalProjection;

            // Adjust the scaling factors to compensate for the 'loss' of screen rect
            // TODO: The '1' I hardcoded here, is this perhaps rect.width?
            modifiedProjection[0, 0] /= 1 - currentCamera.rect.x; 
            modifiedProjection[1, 1] /= 1 - currentCamera.rect.y;

            modifiedProjection[0, 2] = currentCamera.rect.x / aspectRatio;
            // TODO: Berekening hierboven is correct, nu nog die hieronder :)
            modifiedProjection[1, 2] = verticalShift + 1.0f;

            currentCamera.projectionMatrix = modifiedProjection;
        }
    }
}