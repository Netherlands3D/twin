using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Incremental point cloud renderer with simple view-dependent LOD:
/// - Stores all incoming LAS points in memory (positions + colors).
/// - Only uploads a subset of points to the mesh depending on camera zoom:
///     * Overview mode  : evenly sampled subset over whole cloud.
///     * Detail mode    : points within a radius around the camera in XZ.
/// This avoids rendering millions of points at once in WebGL.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class IncrementalPointCloud : MonoBehaviour
{
    // Full dataset (CPU side, all points ever streamed)
    private readonly List<Vector3> _allPositions = new List<Vector3>();
    private readonly List<Color32> _allColors = new List<Color32>();

    // Currently rendered subset
    private readonly List<Vector3> _curPositions = new List<Vector3>();
    private readonly List<Color32> _curColors = new List<Color32>();

    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    [Header("Display")]
    public Material pointMaterial;

    [Tooltip("Automatically move point cloud so its center is at (0,0,0) once (re)built.")]
    public bool recenter = true;

    [Header("LOD Settings")]
    [Tooltip("Maximum points when zoomed out (overview).")]
    public int maxPointsOverview = 50000;

    [Tooltip("Maximum points when zoomed in (detail region).")]
    public int maxPointsDetail = 250000;

    [Tooltip("Radius (in local XZ units) around camera used for detail mode.")]
    public float detailRadius = 50f;

    [Tooltip("If orthographic, overview when orthographicSize > this.")]
    public float overviewOrthoSize = 150f;

    [Tooltip("If perspective, overview when camera Y position > this.")]
    public float overviewHeight = 200f;

    [Header("Update Thresholds")]
    [Tooltip("Rebuild visible subset when camera moved more than this (world units).")]
    public float cameraMoveThreshold = 10f;

    [Tooltip("Rebuild visible subset when zoom changed more than this.")]
    public float zoomChangeThreshold = 10f;

    [Header("Debug")]
    public bool logStats = false;

    [Tooltip("Camera used to decide LOD. If null, Camera.main is used.")]
    public Camera targetCamera;

    // Internal state
    private bool _firstBuild = true;
    private bool _lodDirty = false;
    private bool _hasCentered = false;

    private Vector3 _lastCamPos;
    private float _lastCamZoomMetric;

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
        if (_meshFilter == null)
            _meshFilter = GetComponent<MeshFilter>();

        if (_meshRenderer == null)
            _meshRenderer = GetComponent<MeshRenderer>();

        if (_mesh == null)
        {
            _mesh = new Mesh
            {
                name = "IncrementalPointCloudMesh"
            };

            // Use 32-bit indices when available (WebGL2 usually supports this)
            _mesh.indexFormat = SystemInfo.supports32bitsIndexBuffer
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;

            _mesh.MarkDynamic();
        }

        _meshFilter.sharedMesh = _mesh;

        if (pointMaterial != null)
            _meshRenderer.sharedMaterial = pointMaterial;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
        {
            _lastCamPos = targetCamera.transform.position;
            _lastCamZoomMetric = GetCameraZoomMetric(targetCamera);
        }
    }

    /// <summary>
    /// Clear everything so the component can be reused.
    /// </summary>
    public void ClearAll()
    {
        _allPositions.Clear();
        _allColors.Clear();
        _curPositions.Clear();
        _curColors.Clear();

        if (_mesh != null)
            _mesh.Clear();

        _firstBuild = true;
        _lodDirty = false;
        _hasCentered = false;
    }

    /// <summary>
    /// Called by the streaming loader for each chunk of LAS points.
    /// Keeps all points in CPU memory; rendering subset is decided by LOD.
    /// </summary>
    public void AddPoints(System.Collections.Generic.IReadOnlyList<LasStreamingParser.LasPoint> points)
    {
        if (points == null || points.Count == 0) return;
        Init();

        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            _allPositions.Add(p.position);
            _allColors.Add(p.color);
        }

        _lodDirty = true;
    }

    void LateUpdate()
    {
        if (targetCamera == null)
        {
            // Try again if camera was created later
            targetCamera = Camera.main;
            if (targetCamera == null) return;
        }

        float zoomMetric = GetCameraZoomMetric(targetCamera);
        Vector3 camPos = targetCamera.transform.position;

        bool camMovedFar =
            Vector3.Distance(camPos, _lastCamPos) >= cameraMoveThreshold;

        bool zoomChanged =
            Mathf.Abs(zoomMetric - _lastCamZoomMetric) >= zoomChangeThreshold;

        if (_firstBuild || _lodDirty || camMovedFar || zoomChanged)
        {
            RebuildLOD(camPos, zoomMetric);

            _lastCamPos = camPos;
            _lastCamZoomMetric = zoomMetric;
            _firstBuild = false;
            _lodDirty = false;
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

    private void RebuildLOD(Vector3 camWorldPos, float zoomMetric)
    {
        if (_allPositions.Count == 0)
        {
            _mesh.Clear();
            return;
        }

        _curPositions.Clear();
        _curColors.Clear();

        if (IsOverview(zoomMetric))
        {
            BuildOverviewSubset();
        }
        else
        {
            BuildDetailSubset(camWorldPos);

            // Fallback: if local region is too sparse, show overview instead
            if (_curPositions.Count < maxPointsOverview / 4)
            {
                BuildOverviewSubset();
            }
        }

        UpdateMesh();

        if (logStats)
        {
            Debug.Log($"[IncrementalPointCloud LOD] total={_allPositions.Count} vis={_curPositions.Count} mode={(IsOverview(zoomMetric) ? "OVERVIEW" : "DETAIL")}");
        }
    }

    private void BuildOverviewSubset()
    {
        int total = _allPositions.Count;

        if (total <= maxPointsOverview)
        {
            _curPositions.AddRange(_allPositions);
            _curColors.AddRange(_allColors);
            return;
        }

        // Evenly sample across the dataset
        int step = Mathf.Max(1, total / maxPointsOverview);
        for (int i = 0; i < total; i += step)
        {
            _curPositions.Add(_allPositions[i]);
            _curColors.Add(_allColors[i]);
        }

        if (_curPositions.Count > maxPointsOverview)
        {
            _curPositions.RemoveRange(maxPointsOverview, _curPositions.Count - maxPointsOverview);
            _curColors.RemoveRange(maxPointsOverview, _curColors.Count - maxPointsOverview);
        }
    }

    private void BuildDetailSubset(Vector3 camWorldPos)
    {
        // Convert camera to local space of the point cloud
        Vector3 camLocal = transform.InverseTransformPoint(camWorldPos);

        int total = _allPositions.Count;
        float radiusSqr = detailRadius * detailRadius;
        int added = 0;

        for (int i = 0; i < total; i++)
        {
            Vector3 p = _allPositions[i];
            float dx = p.x - camLocal.x;
            float dz = p.z - camLocal.z;
            float d2 = dx * dx + dz * dz;

            if (d2 <= radiusSqr)
            {
                _curPositions.Add(p);
                _curColors.Add(_allColors[i]);
                added++;

                if (added >= maxPointsDetail)
                    break;
            }
        }
    }

    private void UpdateMesh()
    {
        if (_mesh == null) Init();
        if (_mesh == null) return;

        int vCount = _curPositions.Count;
        if (vCount == 0)
        {
            _mesh.Clear();
            return;
        }

        _mesh.Clear();

        _mesh.SetVertices(_curPositions);
        _mesh.SetColors(_curColors);

        int[] indices = new int[vCount];
        for (int i = 0; i < vCount; i++)
            indices[i] = i;

        _mesh.SetIndices(indices, MeshTopology.Points, 0, false);
        _mesh.RecalculateBounds();

        if (recenter && !_hasCentered)
        {
            var center = _mesh.bounds.center;
            // place the cloud so its center is at world origin
            transform.position = -center;
            _hasCentered = true;
            Debug.Log($"[IncrementalPointCloud] Recentered to origin with offset {center}");
        }
    }

    /// <summary>
    /// optional helper to restore original position later
    /// </summary>
    public void ResetCenter()
    {
        transform.position = Vector3.zero;
        _hasCentered = false;
    }
}
