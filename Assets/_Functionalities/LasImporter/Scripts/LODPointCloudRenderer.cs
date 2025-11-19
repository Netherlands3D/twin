using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// View-dependent LOD point cloud renderer.
/// - Stores ALL LAS points in memory (positions + colors).
/// - When zoomed out: shows a downsampled overview (maxPointsOverview).
/// - When zoomed in: shows only points within a radius around the camera (maxPointsDetail).
/// - Rebuilds the mesh only when camera moves/zooms enough.
///
/// Plug your LAS streaming loader into AddPoints().
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LODPointCloudRenderer : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;
    public Material pointMaterial;

    [Header("LOD Settings")]
    [Tooltip("Maximum points when zoomed out (overview).")]
    public int maxPointsOverview = 50000;

    [Tooltip("Maximum points when zoomed in (detail region).")]
    public int maxPointsDetail = 250000;

    [Tooltip("Radius (in world units) around camera XZ used for detail mode.")]
    public float detailRadius = 50f;

    [Tooltip("If orthographic, overview when orthographicSize > this.")]
    public float overviewOrthoSize = 150f;

    [Tooltip("If perspective, overview when camera height (Y) > this.")]
    public float overviewHeight = 200f;

    [Header("Update Thresholds")]
    [Tooltip("Rebuild LOD when camera moved more than this distance.")]
    public float cameraMoveThreshold = 10f;

    [Tooltip("Rebuild LOD when camera zoom (ortho size or height) changes more than this.")]
    public float zoomChangeThreshold = 10f;

    [Header("Debug")]
    public bool logStats = false;

    // Full dataset
    private readonly List<Vector3> allPositions = new List<Vector3>();
    private readonly List<Color32> allColors = new List<Color32>();

    // Currently rendered subset
    private readonly List<Vector3> curPositions = new List<Vector3>();
    private readonly List<Color32> curColors = new List<Color32>();

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    // For change detection
    private Vector3 lastCamPos;
    private float lastCamZoomMetric;
    private bool firstBuild = true;
    private bool lodDirty = false;

    void Awake()
    {
        Init();
    }

    void OnEnable()
    {
        Init();
    }

    private void Init()
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (mesh == null)
        {
            mesh = new Mesh
            {
                name = "LODPointCloudMesh"
            };

            // Try to use 32-bit indices (WebGL 2 usually supports this).
            if (SystemInfo.supports32bitsIndexBuffer)
                mesh.indexFormat = IndexFormat.UInt32;
            else
                mesh.indexFormat = IndexFormat.UInt16;

            mesh.MarkDynamic();
        }

        meshFilter.sharedMesh = mesh;
        if (pointMaterial != null)
            meshRenderer.sharedMaterial = pointMaterial;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
        {
            lastCamPos = targetCamera.transform.position;
            lastCamZoomMetric = GetCameraZoomMetric(targetCamera);
        }
    }

    /// <summary>
    /// Called by your LAS streaming loader.
    /// Store all points, and mark LOD as dirty.
    /// </summary>
    public void AddPoints(IReadOnlyList<LasStreamingParser.LasPoint> points)
    {
        if (points == null || points.Count == 0) return;

        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            allPositions.Add(p.position);
            allColors.Add(p.color);
        }

        lodDirty = true; // new data came in, so LOD subset needs rebuild
    }

    /// <summary>
    /// Optional single-point helper if you ever need it.
    /// </summary>
    public void AddPoint(LasStreamingParser.LasPoint p)
    {
        allPositions.Add(p.position);
        allColors.Add(p.color);
        lodDirty = true;
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        float zoomMetric = GetCameraZoomMetric(targetCamera);
        Vector3 camPos = targetCamera.transform.position;

        bool camMovedFar =
            Vector3.Distance(camPos, lastCamPos) >= cameraMoveThreshold;

        bool zoomChanged =
            Mathf.Abs(zoomMetric - lastCamZoomMetric) >= zoomChangeThreshold;

        if (firstBuild || lodDirty || camMovedFar || zoomChanged)
        {
            RebuildLOD(camPos, zoomMetric);

            lastCamPos = camPos;
            lastCamZoomMetric = zoomMetric;
            firstBuild = false;
            lodDirty = false;
        }
    }

    private float GetCameraZoomMetric(Camera cam)
    {
        if (cam.orthographic)
            return cam.orthographicSize;
        else
            return cam.transform.position.y;
    }

    private bool IsOverview(float zoomMetric)
    {
        if (targetCamera != null && targetCamera.orthographic)
            return zoomMetric > overviewOrthoSize;
        else
            return zoomMetric > overviewHeight;
    }

    private void RebuildLOD(Vector3 camPos, float zoomMetric)
    {
        if (allPositions.Count == 0)
        {
            mesh.Clear();
            return;
        }

        curPositions.Clear();
        curColors.Clear();

        if (IsOverview(zoomMetric))
        {
            BuildOverviewSubset();
        }
        else
        {
            BuildDetailSubset(camPos);

            // Fallback if detail region is too sparse
            if (curPositions.Count < maxPointsOverview / 4)
            {
                BuildOverviewSubset();
            }
        }

        UpdateMesh();

        if (logStats)
        {
            Debug.Log($"[LODPointCloudRenderer] total:{allPositions.Count} vis:{curPositions.Count} " +
                      $"mode:{(IsOverview(zoomMetric) ? "OVERVIEW" : "DETAIL")}");
        }
    }

    private void BuildOverviewSubset()
    {
        int total = allPositions.Count;

        if (total <= maxPointsOverview)
        {
            // Just render all
            curPositions.AddRange(allPositions);
            curColors.AddRange(allColors);
            return;
        }

        // Evenly sample points across the entire cloud
        int step = Mathf.Max(1, total / maxPointsOverview);
        for (int i = 0; i < total; i += step)
        {
            curPositions.Add(allPositions[i]);
            curColors.Add(allColors[i]);
        }

        // Clamp to maxPointsOverview in case of rounding
        if (curPositions.Count > maxPointsOverview)
        {
            curPositions.RemoveRange(maxPointsOverview, curPositions.Count - maxPointsOverview);
            curColors.RemoveRange(maxPointsOverview, curColors.Count - maxPointsOverview);
        }
    }

    private void BuildDetailSubset(Vector3 camPos)
    {
        int total = allPositions.Count;
        float radiusSqr = detailRadius * detailRadius;
        int added = 0;

        // Select points close to camera in XZ plane
        for (int i = 0; i < total; i++)
        {
            Vector3 p = allPositions[i];
            float dx = p.x - camPos.x;
            float dz = p.z - camPos.z;
            float d2 = dx * dx + dz * dz;

            if (d2 <= radiusSqr)
            {
                curPositions.Add(p);
                curColors.Add(allColors[i]);
                added++;

                if (added >= maxPointsDetail)
                    break;
            }
        }
    }

    private void UpdateMesh()
    {
        if (mesh == null) Init();
        if (mesh == null) return;

        int vCount = curPositions.Count;

        if (vCount == 0)
        {
            mesh.Clear();
            return;
        }

        mesh.Clear();

        mesh.SetVertices(curPositions);
        mesh.SetColors(curColors);

        int[] indices = new int[vCount];
        for (int i = 0; i < vCount; i++)
            indices[i] = i;

        mesh.SetIndices(indices, MeshTopology.Points, 0, false);
        mesh.RecalculateBounds();
    }

    /// <summary>
    /// Clears all stored data and mesh.
    /// </summary>
    public void ClearAll()
    {
        allPositions.Clear();
        allColors.Clear();
        curPositions.Clear();
        curColors.Clear();
        if (mesh != null) mesh.Clear();
        firstBuild = true;
        lodDirty = false;
    }
}
