using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.B3DM;
using Netherlands3D.Coordinates;
using Netherlands3D.SubObjects;
using UnityEngine;

public class TestGltfSubObjects : MonoBehaviour
{
    [SerializeField] string url = "https://api.pdok.nl/kadaster/3d-basisvoorziening/ogc/v1_0/collections/gebouwen/t/9/179/222.glb";
    [SerializeField] private Material material;

    [ContextMenu("Load")]
    public void Load()
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
        parsedGltf.ParseSubObjects(this.transform);

        //Offset so object is in world center
        foreach(Transform child in this.transform)
        { 
            if(child.TryGetComponent(out MeshRenderer meshRenderer))
            {
                child.transform.position = Vector3.zero;
                
                //Apply our material to all materials
                meshRenderer.materials = Enumerable.Repeat(material, meshRenderer.materials.Length).ToArray();

                //Add collider
                child.gameObject.AddComponent<MeshCollider>();
                var meshFilter = child.gameObject.GetComponent<MeshFilter>();
                // Get the source mesh from the source GameObject
                Mesh sourceMesh = meshFilter.sharedMesh;

                Dictionary<string,Color> randomColors = new();

                if(child.TryGetComponent<ObjectMapping>(out ObjectMapping objectMapping))
                {
                    foreach(var item in objectMapping.items)
                    {
                        var id = item.objectID;
                        var vertexStartIndex = item.firstVertex;
                        var vertexCount = item.verticesLength;
                        
                        //add new random color for this object
                        randomColors.Add(id, Random.ColorHSV());
                    }

                    //Apply colors to all subobjects
                    GeometryColorizer.InsertCustomColorSet(0, randomColors);
                }
            }
        }
    }    
}
