using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.ObjImporter.General.GameObjectDataSet;
using UnityEngine;
using Netherlands3D.ObjImporter.ParseOBJ;

namespace Netherlands3D.ObjImporter.General
{
    public class CreateGameObjects : MonoBehaviour
    {

        [HideInInspector] GameObjectDataSet.GameObjectDataSet gameObjectData;

        [HideInInspector] Material BaseMaterial;

        [HideInInspector] public System.Action<float> BroadcastProgressPercentage;
        int gameobjectindex;
        int totalgameobjects;


        System.DateTime time;

        GameObject parentobject;
        Dictionary<string, Material> createdMaterials = new Dictionary<string, Material>();
        Vector3List vertices = new Vector3List();
        Vector3List normals = new Vector3List();
        Vector2List uvs = new Vector2List();

        intList indices = new intList();

        bool gameObjectCreated = false;

        //GameObject createdGameObject;
        Mesh createdMesh;

        System.Action<GameObject> callback;

        public int maxParseMillisecondsPerFrame = 400;

        void BroadCastProgress()
        {
            float progress = 100 * gameobjectindex / totalgameobjects;
            if (BroadcastProgressPercentage != null) BroadcastProgressPercentage(progress);
        }

        public void Create(GameObjectDataSet.GameObjectDataSet gameobjectDataset, Material basematerial,
            System.Action<GameObject> callbackToFunction = null)
        {
            gameObjectData = gameobjectDataset;
            BaseMaterial = basematerial;
            callback = callbackToFunction;
            time = System.DateTime.UtcNow;
            parentobject = new GameObject();
            parentobject.name = gameObjectData.name;
            parentobject.transform.position = gameobjectDataset.Origin;
            totalgameobjects = gameObjectData.gameObjects.Count;
            gameobjectindex = 0;
            StartCoroutine(createGameObjects());
        }

        IEnumerator createGameObjects()
        {
            (int longestVertexCount, int longestIndexCount) = GetLongestFiles(gameObjectData.gameObjects);
            Vector3[] vertices = new Vector3[longestVertexCount];
            Vector2[] uvs = new Vector2[longestVertexCount];
            int[] indices = new int[longestIndexCount];
            
            if (gameObjectData.gameObjects.Count == 1)
            {
                yield return StartCoroutine(AddGameObject(gameObjectData.gameObjects[0], vertices, uvs, indices, parentobject));
                parentobject.name = gameObjectData.name;
            }
            else
            {
                foreach (var gameobjectdata in gameObjectData.gameObjects)
                {
                    gameobjectindex++;
                    BroadCastProgress();
                    gameObjectCreated = false;
                    StartCoroutine(AddGameObject(gameobjectdata, vertices, uvs, indices));
                    while (gameObjectCreated == false)
                    {
                        yield return null;
                    }
                }
            }

            if (callback != null)
            {
                callback(parentobject);
            }

            parentobject = null;
        }

        private (int, int) GetLongestFiles(List<GameObjectData> gameObjects)
        {
            var largestVerts = 0;
            var largestIndices = 0;
            
            foreach (var gameObjectData in gameObjects)
            {
                vertices.SetupReading(gameObjectData.meshdata.vertexFileName);
                indices.SetupReading(gameObjectData.meshdata.indicesFileName);
                
                int vertSize = vertices.Count();
                int indicesSize = indices.numberOfVertices();
                
                if (vertSize > largestVerts)
                    largestVerts = vertSize;

                if (indicesSize > largestIndices)
                    largestIndices = indicesSize;
                    
                vertices.EndReading();
                indices.EndReading();
            }

            return (largestVerts, largestIndices);
        }


        IEnumerator AddGameObject(GameObjectData gameobjectdata, Vector3[] allocatedVertexArray, Vector2[] allocatedUvArray, int[] allocatedIndicesArray, GameObject GameObject = null)
        {
            GameObject gameobject;
            if (GameObject != null)
            {
                gameobject = GameObject;
            }
            else
            {
                gameobject = new GameObject();
            }

            gameobject.name = gameobjectdata.name;
            gameobject.transform.SetParent(parentobject.transform, false);

            yield return StartCoroutine(CreateMesh(gameobjectdata.meshdata, allocatedVertexArray, allocatedUvArray, allocatedIndicesArray));

            if (createdMesh is null)
            {
                gameObjectCreated = true;
                Destroy(gameobject);
                yield break;
            }

            MeshFilter mf = gameobject.AddComponent<MeshFilter>();
            mf.sharedMesh = createdMesh;
            MeshRenderer mr = gameobject.AddComponent<MeshRenderer>();
            List<Material> materiallist = new List<Material>();

            int submeshcount = gameobjectdata.meshdata.submeshes.Count;
            materiallist.Capacity = submeshcount;
            for (int i = 0; i < submeshcount; i++)
            {
                materiallist.Add(getMaterial(gameobjectdata.meshdata.submeshes[i].materialname));
            }

            mr.materials = materiallist.ToArray();
            gameObjectCreated = true;
        }

        Material getMaterial(string materialname)
        {
            Material returnmaterial;
            if (createdMaterials.ContainsKey(materialname))
            {
                returnmaterial = createdMaterials[materialname];
            }
            else
            {
                returnmaterial = new Material(BaseMaterial);
                returnmaterial.name = materialname;
                for (int i = 0; i < gameObjectData.materials.Count; i++)
                {
                    if (gameObjectData.materials[i].Name == materialname)
                    {
                        returnmaterial.name = gameObjectData.materials[i].DisplayName;
                        returnmaterial.color = gameObjectData.materials[i].Diffuse;

                        // Do we have a texture to apply?
                        if (gameObjectData.materials[i].DiffuseTex != null)
                        {
                            returnmaterial.mainTexture = gameObjectData.materials[i].DiffuseTex;
                        }

                        createdMaterials.Add(materialname, returnmaterial);
                    }
                }
            }

            return returnmaterial;
        }


        IEnumerator CreateMesh(MeshData meshdata, Vector3[] allocatedVertexArray, Vector2[] allocatedUvArray, int[] allocatedIndicesArray)
        {
            bool hasnormals = false;
            createdMesh = new Mesh();
            createdMesh.Clear();

            vertices.SetupReading(meshdata.vertexFileName);
            uvs.SetupReading(meshdata.uvFileName);

            int vertexcount = vertices.Count();
            if (vertexcount == 0)
            {
                Debug.Log(meshdata.name + "has no vertices");
                Destroy(createdMesh);
                vertices.EndReading();
                vertices.RemoveData();

                uvs.EndReading();
                uvs.RemoveData();

                yield break;
            }

            vertices.ReadAllItems(allocatedVertexArray);
            createdMesh.SetVertices(allocatedVertexArray, 0, vertexcount);

            uvs.ReadAllItems(allocatedUvArray);
            createdMesh.SetUVs(0, allocatedUvArray, 0, vertexcount);
            
            uvs.EndReading();
            uvs.RemoveData();

            vertices.EndReading();
            vertices.RemoveData();

            if ((DateTime.UtcNow - time).TotalMilliseconds > maxParseMillisecondsPerFrame)
            {
                yield return null;
                time = DateTime.UtcNow;
            }     
            
            // add indices
            indices.SetupReading(meshdata.indicesFileName);
            int indexcount = indices.numberOfVertices();
            
            indices.ReadAllItems(allocatedIndicesArray);
            createdMesh.SetIndexBufferParams(indexcount, UnityEngine.Rendering.IndexFormat.UInt32);
            createdMesh.SetIndexBufferData(allocatedIndicesArray, 0, 0, indexcount);
            
            indices.EndReading();
            indices.RemoveData();

            if ((DateTime.UtcNow - time).TotalMilliseconds > maxParseMillisecondsPerFrame)
            {
                yield return null;
                time = DateTime.UtcNow;
            }
            
            // add normals
            if (meshdata.normalsFileName != "")
            {
                normals.SetupReading(meshdata.normalsFileName);
                int normalscount = normals.Count();

                // Only check normals if > 0. 
                if (normalscount > 0)
                {
                    if (normalscount == vertexcount)
                    {
                        hasnormals = true;
                        
                        normals.ReadAllItems(allocatedVertexArray);
                        createdMesh.SetNormals(allocatedVertexArray, 0, normalscount);
                        
                        if ((DateTime.UtcNow - time).TotalMilliseconds > maxParseMillisecondsPerFrame)
                        {
                            yield return null;
                            time = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        normals.EndReading();
                        normals.RemoveData();
                        Debug.Log(meshdata.name + "number of normals != number of vertices");
                        Destroy(createdMesh);
                        yield break;
                    }
                }
                
                normals.EndReading();
                normals.RemoveData();
            }

            createdMesh.subMeshCount = meshdata.submeshes.Count;
            for (int i = 0; i < meshdata.submeshes.Count; i++)
            {
                UnityEngine.Rendering.SubMeshDescriptor smd = new UnityEngine.Rendering.SubMeshDescriptor();
                smd.indexStart = (int)meshdata.submeshes[i].startIndex;
                smd.indexCount = (int)meshdata.submeshes[i].Indexcount;
                smd.topology = MeshTopology.Triangles;
                //              smd.baseVertex = sm.Value.startVertex;
                //              smd.vertexCount = sm.Value.vertexCount;
                createdMesh.SetSubMesh(i, smd);
            }

            if (hasnormals == false) // Calculate normals using Unity if they are not read from the file, or the file does not have the same number of normals as vertices:
            {
                createdMesh.RecalculateNormals();
            }

            if ((DateTime.UtcNow - time).TotalMilliseconds > maxParseMillisecondsPerFrame)
            {
                yield return null;
                time = DateTime.UtcNow;
            }

            createdMesh.RecalculateBounds();
        }
    }
}