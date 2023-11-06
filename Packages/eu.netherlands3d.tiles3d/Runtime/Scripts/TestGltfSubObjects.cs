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
    private async void GotGltfContent(ParsedGltf parsedGltf)
    {
        //Spawn gltf scenes
        await parsedGltf.SpawnGltfScenes(this.transform);

        //Check if mesh features addon is used to define subobjects
        parsedGltf.ParseSubObjects();

        //Offset using rtcCenter
        foreach(Transform child in this.transform)
        { 
            //List all components on child
            foreach(var component in child.GetComponents<Component>())
            {
                Debug.Log(component.name);
                //ALso log component type name
                Debug.Log(component.GetType().Name);
            }

            if(child.TryGetComponent(out MeshRenderer meshRenderer))
            {
                Debug.Log(meshRenderer.name);
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
