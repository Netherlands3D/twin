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
        public ScatterSettingsPropertySection propertyPanelPrefab; //todo: find a better way to reference this.

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
            if (onPointsGeneratedCallback == null)
                return;

            StartCoroutine(GenerateScatterPointsCoroutine(polygon, density, scatter, angle, onPointsGeneratedCallback));
        }

        private IEnumerator GenerateScatterPointsCoroutine(CompoundPolygon polygon, float density, float scatter, float angle, System.Action<List<Vector3>> onPointsGeneratedCallback)
        {
            float cellSize = 1f / Mathf.Sqrt(density);
            print("cell size: " + cellSize);
            print("max random offset: " + (scatter * cellSize));
            print("polygon bounds: " + polygon.Bounds.size);
            var gridPoints = CompoundPolygon.GenerateGridPoints(polygon, cellSize, angle, out var gridBounds);
            print("gridbounds : " + gridBounds.size);
         
            polyBounds = polygon.Bounds;
            this.gridBounds = gridBounds;
            this.gridCellSize = cellSize;

            //todo: rotate grid in the transform matrix after generation?

            yield return new WaitForEndOfFrame(); //wait for new polygon mesh to be created in case this function was coupled to the same event as the polygon mesh generation and this would be called before the mesh creation.
            CreateRenderTexture(gridBounds, cellSize, GridSampleSize); //todo: polygon should be rendered with an outline to include the random offset of points outside the polygon that due to the random offset will be shifted inside the polygon.
            AlignCameraToPolygon(depthCamera, gridBounds);
            // RenderDepthCamera();
            yield return new WaitForEndOfFrame(); //wait for rendering to complete
            RenderDepthCamera(); //don't know why it is needed to render twice, but not doing so causes unexpected behaviour
            yield return new WaitForEndOfFrame(); //wait for rendering to complete

            //sample texture at points to get random offset and add random offset to world space points. Sample texture at new point to see if it is inside the poygon and if so to get the height.
            var offsetPoints = AddRandomOffsetAndSampleHeight(gridPoints, gridBounds, GridSampleSize, scatter, cellSize);
            //todo somehow sample random scale?

            onPointsGeneratedCallback?.Invoke(offsetPoints);
        }

        private Bounds polyBounds;
        private Bounds gridBounds;
        private float gridCellSize;
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(polyBounds.center, polyBounds.size);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(gridBounds.center, gridBounds.size);
            var width = Mathf.CeilToInt(1f * (gridBounds.size.x + 2 * gridCellSize)); //add 2*maxRandomOffset to include the max scatter range on both sides
            var height = Mathf.CeilToInt(1f * (gridBounds.size.z + 2 * gridCellSize));
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(gridBounds.center, new Vector3(width, 0, height));
        }

        /// <summary>
        /// This function will add a random offset to the grid points, and sample the new point's height and whether or not it falls inside or outside of the polygon.
        /// </summary>
        /// <param name="worldPoints">List of world points to process</param>
        /// <param name="gridBounds">Bounds of the world points</param>
        /// <param name="gridSampleSize">How many samples the texture has per square world unit</param>
        /// <param name="randomness">How much scatter to apply in a Range from 0 (no scatter) to 1 (max scatter)</param>
        /// <param name="gridCellSize">size of a single grid cell</param>
        /// <returns>List of offset points with the sampled height</returns>
        private List<Vector3> AddRandomOffsetAndSampleHeight(Vector2[] worldPoints, Bounds gridBounds, float gridSampleSize, float randomness, float gridCellSize)
        {
            var points = new List<Vector3>(worldPoints.Length);
            var boundsCenter2D = new Vector2(gridBounds.center.x, gridBounds.center.z);
            var boundsExtents2D = new Vector2(gridBounds.extents.x, gridBounds.extents.z);
            var scatterBoundsExtents2D = boundsExtents2D + new Vector2(gridCellSize, gridCellSize); //extents only need to be expanded by 1 cellSize, since the extents are half the size and the size is expanded by 2*cellSize in CreateRenderTexture
            var pointSamplePositionOffset = -boundsCenter2D + boundsExtents2D;
            var scatterPointSamplePositionOffset = -boundsCenter2D + scatterBoundsExtents2D; //scattered bounds have the same center point as unscattered

            var pixels = samplerTexture.GetPixels();
            print("pixelcount: " + pixels.Length);
            var textureWidth = samplerTexture.width;
            var startTime = DateTime.UtcNow;
            for (int i = 0; i < worldPoints.Length; i++)
            {
                var originalWorldPoint = worldPoints[i];
                int originalXInPixelSpace = (int)((originalWorldPoint.x + pointSamplePositionOffset.x) * gridSampleSize); //casting is more efficient than Mathf.FloorToInt
                int originalYInPixelSpace = (int)((originalWorldPoint.y + pointSamplePositionOffset.y) * gridSampleSize);

                int index = originalYInPixelSpace * textureWidth + originalXInPixelSpace;

                if (index >= pixels.Length)
                {
                    print("index out of bounds: " + index);
                    continue;
                }

                var colorSample = pixels[index];
                float randomOffsetX = (colorSample.r - 0.5f) * randomness * gridCellSize; //range [-0.5*gridCellSize, 0.5*gridCellSize]
                float randomOffsetY = (colorSample.g - 0.5f) * randomness * gridCellSize;

                float scatteredPointX = originalWorldPoint.x + randomOffsetX;
                float scatteredPointY = originalWorldPoint.y + randomOffsetY;


                int scatteredXInPixelSpace = (int)((scatteredPointX + scatterPointSamplePositionOffset.x) * gridSampleSize);
                int scatteredYInPixelSpace = (int)((scatteredPointY + scatterPointSamplePositionOffset.y) * gridSampleSize);

                index = scatteredYInPixelSpace * textureWidth + scatteredXInPixelSpace;
                if (index >= pixels.Length || index < 0)
                {
                    print("scatter index out of bounds: " + index);
                    continue;
                }

                var newColorSample = pixels[index];


                if (newColorSample.a < 0.5f) //new sampled color does not have an alpha value, so it falls outside of the polygon. Therefore this point can be skipped. This wil clip out any points outside of the polygon
                    continue;

                //todo: in WebGL the depth is inverted.                
                var offsetPoint = new Vector3(scatteredPointX, newColorSample.b, scatteredPointY);

                points.Add(offsetPoint);
            }

            return points;
        }

        /// <summary>
        /// Create a render texture for the camera to render to, using the to polygon size + max random offset on all sides.
        /// </summary>
        /// <param name="polygon">the polygon to fit to</param>
        /// <param name="maxRandomOffset">the maximum extra space around an edge of the polygon bounds to include, for example when scattering points with a randomness that might need data outside of the polygon bounds</param>
        /// <param name="gridSampleSize">how many samples per square world unit should be taken in the texture</param>
        private void CreateRenderTexture(Bounds gridBounds, float gridCellSize, float gridSampleSize)
        {
            var width = Mathf.CeilToInt(gridSampleSize * (gridBounds.size.x + 2 * gridCellSize)); //add 2*maxRandomOffset to include the max scatter range on both sides
            var height = Mathf.CeilToInt(gridSampleSize * (gridBounds.size.z + 2 * gridCellSize));
            var renderTexture = new RenderTexture(width, height, GraphicsFormat.R32G32B32A32_SFloat, GraphicsFormat.None);
            print("created texture: " + width + "x" + height + " pixelCount: " + width * height);
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