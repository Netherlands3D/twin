using System.Collections;
using UnityEngine;

public class LasStreamingLoader : MonoBehaviour
{
    [Tooltip("Drop a .las file here (TextAsset, import as Binary)")]
    public TextAsset lasFile;

    [Tooltip("Point cloud target that will display points as they arrive")]
    public IncrementalPointCloud pointCloud;

    [Tooltip("How many points to read per frame")]
    public int pointsPerFrame = 5000;

    private LasStreamingParser _parser;

    void Start()
    {
        if (lasFile == null || pointCloud == null)
        {
            Debug.LogError("Assign lasFile and pointCloud");
            return;
        }

        _parser = new LasStreamingParser(lasFile.bytes);
        StartCoroutine(StreamPoints());
    }

    private IEnumerator StreamPoints()
    {
        while (!_parser.Finished)
        {
            var chunk = _parser.ReadNextPoints(pointsPerFrame);
            pointCloud.AddPoints(chunk);

            // let the frame finish so viewer doesn't freeze
            yield return null;
        }

        _parser.Dispose();
        Debug.Log("Finished streaming LAS.");
    }
}
