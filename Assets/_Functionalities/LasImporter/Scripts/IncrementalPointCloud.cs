using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Incremental point cloud renderer with simple, global, stride-based LOD:
/// - Stores all incoming LAS points in memory (positions + colors).
/// - Uses 3 LOD levels based on camera zoom:
///     * Overview : light sample across entire cloud.
///     * Medium   : denser sample across entire cloud.
///     * Detail   : densest sample across entire cloud.
/// - Each LOD just picks every N-th point (global stride),
///   so it's very cheap and WebGL friendly.
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

    [Header("LOD Point Caps")]
    [Tooltip("Maximum points when zoomed out (overview).")]
    public int maxPointsOverview = 30000;

    [Tooltip("Maximum points when in medium zoom level.")]
    public int maxPointsMedium = 100000;

    [Tooltip("Maximum points when zoomed in (detail).")]
    public int maxPointsDetail = 250000;

    [Header("LOD thresholds (Perspective)")]
    [Tooltip("Camera height above which we are in OVERVIEW mode (perspective camera).")]
    public float overviewHeightFar = 200f;

    [Tooltip("Camera height above which we are in MEDIUM mode (perspective camera). Below this is DETAIL.")]
    public float overviewHeightMedium = 80f;

    [Header("LOD thresholds (Orthographic)")]
    [Tooltip("Ortho size above which we are in OVERVIEW mode.")]
    public float overviewOrthoFar = 150f;

    [Tooltip("Ortho size above which we are in MEDIUM mode. Below this is DETAIL.")]
    public float overviewOrthoMedium = 60f;

    [Header("Update Thresholds")]
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

    private float _lastCamZoomMetric;

    private enum LodMode { Overview, Medium, Detail }

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
        bool zoomChanged =
            Mathf.Abs(zoomMetric - _lastCamZoomMetric) >= zoomChangeThreshold;

        if (_firstBuild || _lodDirty || zoomChanged)
        {
            RebuildLOD(zoomMetric);

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

    private LodMode GetLodMode(float zoomMetric)
    {
        if (targetCamera != null && targetCamera.orthographic)
        {
            if (zoomMetric > overviewOrthoFar)
                return LodMode.Overview;
            if (zoomMetric > overviewOrthoMedium)
                return LodMode.Medium;
            return LodMode.Detail;
        }
        else
        {
            if (zoomMetric > overviewHeightFar)
                return LodMode.Overview;
            if (zoomMetric > overviewHeightMedium)
                return LodMode.Medium;
            return LodMode.Detail;
        }
    }

    private void RebuildLOD(float zoomMetric)
    {
        if (_allPositions.Count == 0)
        {
            _mesh.Clear();
            return;
        }

        _curPositions.Clear();
        _curColors.Clear();

        LodMode mode = GetLodMode(zoomMetric);
        int cap = maxPointsDetail;

        switch (mode)
        {
            case LodMode.Overview:
                cap = maxPointsOverview;
                break;
            case LodMode.Medium:
                cap = maxPointsMedium;
                break;
            case LodMode.Detail:
                cap = maxPointsDetail;
                break;
        }

        BuildStrideSubset(cap);

        UpdateMesh();

        if (logStats)
        {
            Debug.Log($"[IncrementalPointCloud LOD] total={_allPositions.Count} vis={_curPositions.Count} mode={mode}");
        }
    }

    /// <summary>
    /// Global, stride-based sampling:
    /// pick every N-th point so we never exceed "cap" points,
    /// spread across the whole dataset.
    /// </summary>
    private void BuildStrideSubset(int cap)
    {
        int total = _allPositions.Count;

        if (total <= cap)
        {
            _curPositions.AddRange(_allPositions);
            _curColors.AddRange(_allColors);
            return;
        }

        int step = Mathf.Max(1, total / cap);

        // Small offset so we don't always start at the very first point.
        int offset = 0;

        for (int i = offset; i < total; i += step)
        {
            _curPositions.Add(_allPositions[i]);
            _curColors.Add(_allColors[i]);
        }

        if (_curPositions.Count > cap)
        {
            _curPositions.RemoveRange(cap, _curPositions.Count - cap);
            _curColors.RemoveRange(cap, _curColors.Count - cap);
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
