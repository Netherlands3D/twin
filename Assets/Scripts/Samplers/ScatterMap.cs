using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(Camera))]
    public class ScatterMap : MonoBehaviour
    {
        public static ScatterMap Instance { get; private set; }

        private Camera depthCamera;
        private Texture2D samplerTexture;

        public float GridSampleSize = 1f; //how many pixels per square meter should be used in the texture for sampling?

        [SerializeField] private RawImage debugImageRaw; //todo: delete me
        [SerializeField] private RawImage debugImageSample; //todo: delete me

        // public UnityEvent<List<Vector3>> ScatterPointsGenerated;

        private void Awake()
        {
            depthCamera = GetComponent<Camera>();
            if (Instance)
                Debug.LogError("There should only be one ScatterMap Instance. Having multiple may result in unexpected behaviour.", gameObject);
            else
                Instance = this;
        }

        void Start()
        {
            //We will only render on demand using camera.Render()
            depthCamera.enabled = false;
        }

        private void OnDestroy()
        {
            Destroy(samplerTexture);
        }


        public void RenderDepthCamera()
        {
            //Read pixels from the depth texture
            depthCamera.Render();
            RenderTexture.active = depthCamera.targetTexture;
            //Create a texture with 32 bits per channel that we can sample  from. this is needed because the RenderTexture only exists on the GPU
            samplerTexture = new Texture2D(depthCamera.targetTexture.width, depthCamera.targetTexture.height, TextureFormat.RGBAFloat, false);
            samplerTexture.ReadPixels(new Rect(0, 0, depthCamera.targetTexture.width, depthCamera.targetTexture.height), 0, 0);
            samplerTexture.Apply();
            debugImageSample.texture = samplerTexture;
            RenderTexture.active = null;
        }

//         public Vector3 GetDepthCameraWorldPoint()
//         {
//             CalculateAverageDepth();
//
// #if UNITY_EDITOR || !UNITY_WEBGL
//             //WebGL/OpenGL2 raw depth value is inverted
//             totalDepth = 1 - totalDepth;
// #endif
//
//             //Move far clip plane according to camera height to maintain precision (min 100m far clip plane)
//             var clipRangeTotal = Mathf.Max(depthCamera.transform.position.y * 2.0f, 100.0f);
//             depthCamera.farClipPlane = clipRangeTotal;
//
//             //Use camera near and far to determine totalDepth value
//             totalDepth = Mathf.Lerp(depthCamera.nearClipPlane, depthCamera.farClipPlane, totalDepth);
//
//             var worldPoint = depthCamera.transform.position + depthCamera.transform.forward * totalDepth;
//             OnDepthSampled.Invoke(worldPoint);
//
//             return worldPoint;
//         }

        public void GenerateScatterPoints(CompoundPolygon polygon, float density, float scatter, float angle, System.Action<List<Vector3>> onPointsGeneratedCallback)
        {
            // if (onPointsGeneratedCallback == null)
            //     return;

            StartCoroutine(GenerateScatterPointsCoroutine(polygon, density, scatter, angle, onPointsGeneratedCallback));
        }

        private IEnumerator GenerateScatterPointsCoroutine(CompoundPolygon polygon, float density, float scatter, float angle, System.Action<List<Vector3>> onPointsGeneratedCallback)
        {
            float cellSize = 1f / Mathf.Sqrt(density);

            var gridPoints = CompoundPolygon.GenerateGridPoints(polygon, cellSize, angle, out var gridBounds);
            //todo: rotate grid in the transform matrix after generation?

            yield return new WaitForEndOfFrame(); //wait for new polygon mesh to be created in case this function was coupled to the same event as the polygon mesh generation and this would be called before the mesh creation.
            CreateRenderTexture(gridBounds, scatter * cellSize, GridSampleSize); //todo: polygon should be rendered with an outline to include the random offset of points outside the polygon that due to the random offset will be shifted inside the polygon.
            AlignCameraToPolygon(depthCamera, gridBounds);
            RenderDepthCamera();
            yield return new WaitForEndOfFrame(); //wait for rendering to complete
            RenderDepthCamera(); //don't know why it is needed to render twice, but not doing so causes unexpected behaviour
            yield return new WaitForEndOfFrame(); //wait for rendering to complete

            //sample texture at points to get random offset and add random offset to world space points. Sample texture at new point to see if it is inside the poygon and if so to get the height.
            var offsetPoints = AddSampledRandomOffsetAndSampleHeight(gridPoints, gridBounds, GridSampleSize, scatter, cellSize);
            //todo somehow sample random scale?

            onPointsGeneratedCallback?.Invoke(offsetPoints);
        }

        public GameObject testSphere;

        private List<Vector3> AddSampledRandomOffsetAndSampleHeight(List<Vector2> worldPoints, Bounds bounds , float gridSampleSize, float randomness, float gridCellSize)
        {
            var points = new List<Vector3>(worldPoints.Count);
            // var parent = new GameObject("testSpheres").transform;
            var boundsCenter2D = new Vector2(bounds.center.x, bounds.center.z);
            var boundsExtents2D = new Vector2(bounds.extents.x, bounds.extents.z);
            var pointSamplePositionOffset = -boundsCenter2D + boundsExtents2D;
            for (int i = 0; i < worldPoints.Count; i++)
            {
                var worldPointInPixelSpace = (worldPoints[i] +pointSamplePositionOffset) * gridSampleSize;
                var colorSample = samplerTexture.GetPixel(Mathf.FloorToInt(worldPointInPixelSpace.x), Mathf.FloorToInt(worldPointInPixelSpace.y));
                float randomOffsetX = (colorSample.r - 0.5f) * randomness * gridCellSize;
                float randomOffsetY = (colorSample.g - 0.5f) * randomness * gridCellSize;
                var offset = new Vector2(randomOffsetX, randomOffsetY);
                // var offsetUV = uv + new Vector2(colorSample.r, colorSample.g) * gridCellSize;
                // var newColorSample = ReadPixelFromTexture(samplerTexture, offsetUV);

                if (colorSample.a < 0.5f) //new sampled color does not have an alpha value, so it falls outside of the polygon. Therefore this point can be skipped. This wil clip out any points outside of the polygon
                    continue;

                var originalWorldPoint = worldPoints[i];
                var offsetPoint = new Vector3(originalWorldPoint.x /*+ offset.x*/, colorSample.b, originalWorldPoint.y /*+ offset.y*/);

                // var debug = Instantiate(testSphere, offsetPoint, quaternion.identity, parent);

                points.Add(offsetPoint);
            }

            return points;
        }

        /// <summary>
        /// Convert grid space coordinates to texture coordinates
        /// </summary>
        /// <param name="gridPos">position to convert</param>
        /// <param name="boundsCenter">World Position center point of the bounds of the grid</param>
        /// <param name="gridSampleSize">how many samples per square world unit are taken in the texture</param>
        /// <returns></returns>
        private static Vector2 GridToTextureCoord(Texture2D texture, Vector2 gridPos, Vector3 boundsCenter, float gridSampleSize)
        {
            Vector2 localGridPos = gridPos - new Vector2(boundsCenter.x, boundsCenter.z); // Convert to local space relative to the center of the bounds

            var scaledTextureHorizontalExtent = texture.width / 2 / gridSampleSize; //divide by gridSampleSize to account for multiple pixels in the texture per square world unit 
            var scaledTextureVerticalExtent = texture.height / 2 / gridSampleSize;

            float x = Mathf.InverseLerp(-scaledTextureHorizontalExtent, scaledTextureHorizontalExtent, localGridPos.x); // Convert x to [0, 1]
            float y = Mathf.InverseLerp(-scaledTextureVerticalExtent, scaledTextureVerticalExtent, localGridPos.y); // Convert x to [0, 1]
            return new Vector2(x, y);
        }

        /// <summary>
        /// Create a render texture for the camera to render to, using the to polygon size + max random offset on all sides.
        /// </summary>
        /// <param name="polygon">the polygon to fit to</param>
        /// <param name="maxRandomOffset">the maximum extra space around an edge of the polygon bounds to include, for example when scattering points with a randomness that might need data outside of the polygon bounds</param>
        /// <param name="gridSampleSize">how many samples per square world unit should be taken in the texture</param>
        private void CreateRenderTexture(Bounds bounds, float maxRandomOffset, float gridSampleSize)
        {
            var width = Mathf.CeilToInt(gridSampleSize * bounds.size.x + 2 * maxRandomOffset); //add 2*maxRandomOffset to include the max scatter range on both sides
            var height = Mathf.CeilToInt(gridSampleSize * bounds.size.z + 2 * maxRandomOffset);
            print("texture dimensions: " + width + " x " + height);
            var renderTexture = new RenderTexture(width, height, GraphicsFormat.R32G32B32A32_SFloat, GraphicsFormat.None);
            depthCamera.targetTexture = renderTexture;
            debugImageRaw.texture = renderTexture;

            if (depthCamera.targetTexture.width > 4096 || depthCamera.targetTexture.height > 4096)
                throw new ArgumentOutOfRangeException("Texture size should not be higher than 4096");
            //todo: cap resolution to max 4096 x 4096 and render the texture in batches if it is higher
        }

        /// <summary>
        /// Align the camera to the polygon bounds
        /// </summary>
        /// <param name="camera">Camera to align. The camera must be orthographic to set the size properly</param>
        /// <param name="bounds">Polygon to align to. The camera orthographic size will be set to the polygon height (z) value.</param>
        public void AlignCameraToPolygon(Camera camera, Bounds bounds)
        {
            camera.transform.position = new Vector3(bounds.center.x, camera.transform.position.y, bounds.center.z);
            camera.orthographicSize = bounds.extents.z;
        }
    }
}