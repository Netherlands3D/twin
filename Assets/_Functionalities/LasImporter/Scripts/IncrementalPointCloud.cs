using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Incrementally builds a MeshTopology.Points mesh from streamed LAS points.
/// Designed to be WebGL-friendly (16-bit indices, no exotic features).
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class IncrementalPointCloud : MonoBehaviour
{
    // Internal buffers
    private readonly List<Vector3> _positions = new List<Vector3>();
    private readonly List<Color32> _colors = new List<Color32>();
    private Mesh _mesh;

    [Header("Display")]
    public Material pointMaterial;

    [Tooltip("Automatically move point cloud so its center is at (0,0,0) once it's built.")]
    public bool recenter = true;

    [Tooltip("Auto-update mesh when new points are added.")]
    public bool autoUpdateMesh = true;

    [Tooltip("Rebuild mesh after this many new points are added.")]
    public int updateBatchSize = 10000;

    // WebGL-safe limit (UInt16 indices)
    private const int MaxVertices = 65000;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private bool _meshDirty;
    private bool _hasCentered;
    private int _lastUpdateVertexCount;

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
                name = "IncrementalPointCloudMesh",
                indexFormat = IndexFormat.UInt16    // WebGL-safe
            };
            _mesh.MarkDynamic();
        }

        _meshFilter.sharedMesh = _mesh;

        if (pointMaterial != null)
            _meshRenderer.sharedMaterial = pointMaterial;
    }

    /// <summary>
    /// Clear everything (for re-use).
    /// </summary>
    public void Clear()
    {
        _positions.Clear();
        _colors.Clear();

        if (_mesh != null)
            _mesh.Clear();

        _meshDirty = false;
        _hasCentered = false;
        _lastUpdateVertexCount = 0;
    }

    /// <summary>
    /// Add a single point (convenience).
    /// </summary>
    public void AddPoint(LasStreamingParser.LasPoint point)
    {
        var list = new List<LasStreamingParser.LasPoint>(1) { point };
        AddPoints(list);
    }

    /// <summary>
    /// Add a chunk of LAS points.
    /// This is what your LasStreamingLoader / LASSpawner calls.
    /// </summary>
    public void AddPoints(IReadOnlyList<LasStreamingParser.LasPoint> points)
    {
        if (points == null || points.Count == 0) return;

        // Make sure mesh exists
        Init();

        int canAdd = Mathf.Min(points.Count, MaxVertices - _positions.Count);
        if (canAdd <= 0)
        {
            Debug.LogWarning($"[IncrementalPointCloud] Vertex limit ({MaxVertices}) reached, skipping remaining points.");
            return;
        }

        for (int i = 0; i < canAdd; i++)
        {
            var p = points[i];
            _positions.Add(p.position);
            _colors.Add(p.color);
        }

        if (canAdd < points.Count)
        {
            Debug.LogWarning($"[IncrementalPointCloud] Truncated chunk: added {canAdd} of {points.Count} points due to 16-bit index limit.");
        }

        _meshDirty = true;

        if (autoUpdateMesh)
        {
            int diff = _positions.Count - _lastUpdateVertexCount;
            if (diff >= updateBatchSize)
            {
                UpdateMesh();
            }
        }
    }

    void LateUpdate()
    {
        // Fallback: ensure mesh eventually updates even if batch threshold not hit
        if (_meshDirty && !autoUpdateMesh)
        {
            UpdateMesh();
        }
    }

    private void UpdateMesh()
    {
        if (_mesh == null) Init();
        if (_mesh == null) return;

        int vCount = _positions.Count;
        if (vCount == 0)
        {
            _mesh.Clear();
            _meshDirty = false;
            return;
        }

        // Set vertex + color buffers
        _mesh.SetVertices(_positions);
        _mesh.SetColors(_colors);

        // Generate simple 0..N-1 indices (UInt16 by default)
        int[] indices = new int[vCount];
        for (int i = 0; i < vCount; i++)
            indices[i] = i;

        _mesh.SetIndices(indices, MeshTopology.Points, 0, false);

        // Bounds are important for culling!
        _mesh.RecalculateBounds();

        if (recenter && !_hasCentered)
        {
            RecenterToBounds();
            _hasCentered = true;
        }

        _lastUpdateVertexCount = vCount;
        _meshDirty = false;

        Debug.Log($"[IncrementalPointCloud] Updated mesh: {vCount} points.");
    }

    private void RecenterToBounds()
    {
        var bounds = _mesh.bounds;
        var center = bounds.center;

        // Move the transform so the cloud is centered around world origin
        transform.position = -center;
        Debug.Log($"[IncrementalPointCloud] Recentered to origin (offset {center}).");
    }

    // Optional helper if you want to undo recentering
    public void ResetCenter()
    {
        transform.position = Vector3.zero;
        _hasCentered = false;
    }
}
