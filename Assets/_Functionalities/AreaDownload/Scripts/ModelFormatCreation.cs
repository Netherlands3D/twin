using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Netherlands3D.MeshClipping;

public struct ObjectData
{
    public ObjectData(string name, List<Vector3> vertices, Material material)
    {
        Name = name;
        Vertices = vertices;
        Material = material;
    }

    public string Name;
    public List<Vector3> Vertices;
    public Material Material;
}

public abstract class ModelFormatCreation : MonoBehaviour
{
    public void StartDownload(LayerMask includedLayers, Bounds selectedAreaBounds, float minClipBoundsHeight, bool destroyOnCompletion = true)
    {
        StartCoroutine(CreateFile(includedLayers, selectedAreaBounds, minClipBoundsHeight, destroyOnCompletion));
    }

    protected abstract IEnumerator CreateFile(LayerMask includedLayers, Bounds selectedAreaBounds, float minClipBoundsHeight, bool destroyOnCompletion = true);

    protected List<ObjectData> GetExportData(LayerMask includedLayers, Bounds selectedAreaBounds, float minClipBoundsHeight)
    {
        var meshClipper = new MeshClipper();
        List<ObjectData> objects = new();

        //Find all gameobjects inside the includedLayers
        var meshFilters = FindObjectsOfType<MeshFilter>();

        //check if the meshfilter is inside the included layers
        var meshFiltersInLayers = new List<MeshFilter>();
        foreach (var meshFilter in meshFilters)
        {
            if (includedLayers == (includedLayers | (1 << meshFilter.gameObject.layer)))
            {
                meshFiltersInLayers.Add(meshFilter);
            }
        }

        //Clip all the submeshes found whose object bounds overlap with the selected area bounds
        for (int i = 0; i < meshFiltersInLayers.Count; i++)
        {
            var mesh = meshFiltersInLayers[i].sharedMesh;
            for (int j = 0; j < mesh.subMeshCount; j++)
            {
                var meshFilterGameObject = meshFiltersInLayers[i].gameObject;

                //Set the object name
                var subobjectName = meshFilterGameObject.name;
                Material material = null;
                if (meshFilterGameObject.TryGetComponent<MeshRenderer>(out var meshRenderer))
                {
                    //Make sure renderer overlaps bounds
                    if (!meshRenderer.bounds.Intersects(selectedAreaBounds))
                    {
                        continue;
                    }

                    material = meshRenderer.sharedMaterials[j];
                    subobjectName = material.name.Replace(" (Instance)", "").Split(' ')[0];
                }

                //Fresh start for meshclipper
                var clipBounds = selectedAreaBounds;
                clipBounds.size = new Vector3(clipBounds.size.x, Mathf.Max(clipBounds.size.y, minClipBoundsHeight), clipBounds.size.z);
                meshClipper.SetGameObject(meshFiltersInLayers[i].gameObject);
                meshClipper.ClipSubMesh(clipBounds, j);
                var objectData = new ObjectData(subobjectName, meshClipper.clippedVertices, material);
                objects.Add(objectData);
            }
        }

        return objects;
    }
}