using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.B3DM;
using Netherlands3D.Coordinates;
using UnityEngine;

public class TestGltfSubObjects : MonoBehaviour
{
    [SerializeField] string url = "https://api.pdok.nl/kadaster/3d-basisvoorziening/ogc/v1_0/collections/gebouwen/t/9/179/222.glb";

    [ContextMenu("Go")]
    public void Go()
    {
        Debug.Log("Load test .glb");
        StartCoroutine(
            ImportB3DMGltf.ImportBinFromURL(url, GotGltfContent)
        );
    }

     /// <summary>
    /// After parsing gltf content spawn gltf scenes
    /// </summary>
    private void GotGltfContent(ParsedGltf parsedGltf)
    {
        parsedGltf.SpawnGltfScenes(this.transform);

        //Check if mesh features addon is used to define subobjects
        bool has_EXT_mesh_Features = parsedGltf.gltfImport.GetSourceRoot().extensionsUsed.Contains("EXT_mesh_Features");
        parsedGltf.ParseSubObjects();

        //Offset using rtcCenter
        foreach(Transform child in this.transform)
        { 
            Vector3 unityPosition = CoordinateConverter.ECEFToUnity(new Vector3ECEF(parsedGltf.rtcCenter[0], parsedGltf.rtcCenter[1], parsedGltf.rtcCenter[2]));
            child.position = unityPosition;
        }
    }    
}
