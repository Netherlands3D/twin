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
    public bool recenter = true;

    // WebGL 1 safe cap
    public int maxPoints = 65000;

    private bool _hasCentered = false;

    void Awake()
    {
        _mesh = new Mesh();
        // IMPORTANT: 16-bit indices for WebGL safety
        _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;

        GetComponent<MeshFilter>().sharedMesh = _mesh;

        if (pointMaterial != null)
            GetComponent<MeshRenderer>().sharedMaterial = pointMaterial;
    }

    public void AddPoints(List<LasStreamingParser.LasPoint> points)
    {
        if (points == null || points.Count == 0) return;

        int remaining = maxPoints - _positions.Count;
        if (remaining <= 0) return; // ignore extra for now

        int toAdd = Mathf.Min(remaining, points.Count);

        for (int i = 0; i < toAdd; i++)
        {
            _positions.Add(points[i].position);
            _colors.Add(points[i].color);
        }

        RebuildMesh();

        if (recenter && !_hasCentered)
        {
            var center = _mesh.bounds.center;
            transform.position = -center;
            _hasCentered = true;
        }
    }

    private void RebuildMesh()
    {
        int count = _positions.Count;
        _mesh.Clear();

        _mesh.SetVertices(_positions);
        _mesh.SetColors(_colors);

        // 0..count-1
        var indices = new int[count];
        for (int i = 0; i < count; i++)
            indices[i] = i;

        _mesh.SetIndices(indices, MeshTopology.Points, 0, true);
    }
}