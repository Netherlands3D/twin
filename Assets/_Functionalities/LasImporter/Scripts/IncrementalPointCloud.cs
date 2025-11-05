using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class IncrementalPointCloud : MonoBehaviour
{
    private readonly List<Vector3> _positions = new List<Vector3>();
    private readonly List<Color32> _colors = new List<Color32>();
    private Mesh _mesh;

    [Header("Display")]
    public Material pointMaterial;
    [Tooltip("Automatically move point cloud so its center is at (0,0,0)")]
    public bool recenter = true;

    // internal
    private bool _hasCentered = false;

    void Awake()
    {
        _mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
        GetComponent<MeshFilter>().sharedMesh = _mesh;

        if (pointMaterial != null)
            GetComponent<MeshRenderer>().sharedMaterial = pointMaterial;
    }

    public void AddPoints(List<LasStreamingParser.LasPoint> points)
    {
        if (points == null || points.Count == 0) return;

        foreach (var p in points)
        {
            _positions.Add(p.position);
            _colors.Add(p.color);
        }

        RebuildMesh();

        if (recenter && !_hasCentered)
        {
            CenterToBounds();
            _hasCentered = true;
        }
    }

    private void RebuildMesh()
    {
        int count = _positions.Count;
        _mesh.Clear();
        _mesh.SetVertices(_positions);
        _mesh.SetColors(_colors);

        var indices = new int[count];
        for (int i = 0; i < count; i++)
            indices[i] = i;

        _mesh.SetIndices(indices, MeshTopology.Points, 0, true);
    }

    private void CenterToBounds()
    {
        if (_positions.Count == 0) return;
        var bounds = _mesh.bounds;
        var center = bounds.center;

        // offset the mesh so its center is at world origin
        transform.position = -center;
        Debug.Log($"Point cloud recentered to origin (offset {center})");
    }

    // optional helper to restore original position later
    public void ResetCenter()
    {
        transform.position = Vector3.zero;
        _hasCentered = false;
    }
}
