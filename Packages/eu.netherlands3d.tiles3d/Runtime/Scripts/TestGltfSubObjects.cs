using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.B3DM;
using Netherlands3D.Coordinates;
using UnityEngine;

public class TestGltfSubObjects : MonoBehaviour
{
    [SerializeField] string url = "https://api.pdok.nl/kadaster/3d-basisvoorziening/ogc/v1_0/collections/gebouwen/t/9/179/222.glb";

    [SerializeField] private Material material;

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
        parsedGltf.ParseSubObjects();

        //Offset using rtcCenter
        foreach(Transform child in this.transform)
        { 
            if(child.TryGetComponent(out MeshRenderer meshRenderer))
            {
                child.transform.position = Vector3.zero;
                
                //apply material to all materials
                meshRenderer.materials = Enumerable.Repeat(material, meshRenderer.materials.Length).ToArray();

                //Get mesh colors
                var mesh = meshRenderer.GetComponent<MeshFilter>().sharedMesh;
                                  
                //Create new color array by using parsedGltf.featureTableFloats for the color id from colorDictionary
                var newColors = new Color[mesh.vertexCount];
                for(int i = 0; i < newColors.Length; i++)
                {
                    newColors[i] = parsedGltf.uniqueColors[parsedGltf.featureTableFloats[i]];
                }

                mesh.colors = newColors;
            }
            
        }
    }    
}
