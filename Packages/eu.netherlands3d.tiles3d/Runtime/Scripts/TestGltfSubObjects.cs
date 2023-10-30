using System.Threading.Tasks;
using Netherlands3D.B3DM;
using UnityEngine;

public class TestGltfSubObjects : MonoBehaviour
{
    [SerializeField] string url = "https://api.pdok.nl/kadaster/3d-basisvoorziening/ogc/v1_0/collections/gebouwen/t/9/179/222.glb";

    [ContextMenu("Go")]
    public void Go()
    {
        StartCoroutine(
            ImportB3DMGltf.ImportBinFromURL(url, GotGltfContent)
        );
    }

     /// <summary>
    /// After parsing gltf content spawn gltf scenes
    /// </summary>
    private void GotGltfContent(ParsedGltf parsedGltf){
        Debug.Log(parsedGltf);
    }
}
