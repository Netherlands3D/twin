using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using GLTFast;
using System;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;
using Meshoptimizer;
using Unity.Collections;
using Netherlands3D.Coordinates;
using SimpleJSON;


#if SUBOBJECT
using Netherlands3D.SubObjects;
#endif

namespace Netherlands3D.Tiles3D
{
    [Serializable]
    public class ParsedGltf
    {
        public GltfImport gltfImport;
        public byte[] glbBuffer;
        public byte[] gltfJsonData;
        public double[] rtcCenter = null;
        public CoordinateSystem coordinatesystem;

        private NativeArray<byte> glbBufferNative;
        private NativeArray<byte> destination;
        private NativeSlice<byte> source;

        public Dictionary<int, Color> uniqueColors = new Dictionary<int, Color>();

        JSONNode gltfJsonRoot = null;

        public bool isSupported()
        {
            ReadGLTFJson();
            if (gltfJsonRoot==null)
            {
                Debug.LogError("gltf doesn't contain a valid JSON");
                return false;
            }

            JSONNode extensionsRequiredNode = gltfJsonRoot["extensionsRequired"];
            if (extensionsRequiredNode == null)
            {
                return true;
            }
            int extensionsRequiredCount = extensionsRequiredNode.Count;
            int cesiumRTCIndex = -1;
            for (int ii = 0; ii < extensionsRequiredCount; ii++)
            {
                if (extensionsRequiredNode[ii].Value == "CESIUM_RTC")
                {
                    cesiumRTCIndex = ii;
                    continue;
                }

            }
            if (cesiumRTCIndex < 0)
            {
                return true ;
            }


            return false;
        }

        public List<int> featureTableFloats = new List<int>();
        Transform parentTransform;
        Tile tile;
        /// <summary>
        /// Iterate through all scenes and instantiate them
        /// </summary>
        /// <param name="parent">Parent spawned scenes to Transform</param>
        /// <returns>Async Task</returns>
        /// 
        private void ReadGLTFJson()
        {
            int jsonstart = 20;
            int jsonlength = (glbBuffer[15]) * 256;
            jsonlength = (jsonlength + glbBuffer[14]) * 256;
            jsonlength = (jsonlength + glbBuffer[13]) * 256;
            jsonlength = (jsonlength + glbBuffer[12]);

            string gltfjsonstring = Encoding.UTF8.GetString(glbBuffer, jsonstart, jsonlength);


            if (gltfjsonstring.Length > 0)
            {

                gltfJsonRoot = JSON.Parse(gltfjsonstring);
            }
        }


        public async Task SpawnGltfScenes(Transform parent)
        {
            parentTransform = parent;
            if (gltfImport != null)
            {
                Content parentContent = parent.GetComponent<Content>();
                if (parentContent!=null)
                {
                    tile = parentContent.ParentTile;
                }

                var scenes = gltfImport.SceneCount;

                //Spawn all scenes (InstantiateMainSceneAsync only possible if main scene was referenced in gltf)
                for (int i = 0; i < scenes; i++)
                {
                    await gltfImport.InstantiateSceneAsync(parent, i);
                    var scene = parent.GetChild(i).transform;
                    if (scene == null) continue;

                    //set unitylayer for all gameon=bjects in scene to unityLayer of container
                    foreach (var child in scene.GetComponentsInChildren<Transform>(true)) //getting the Transform components ensures the layer of each recursive child is set 
                    {
                        child.gameObject.layer = parent.gameObject.layer;
                    }
                    PositionGameObject(scene, rtcCenter, tile);
                }
            }
        }
        void PositionGameObject(Transform scene, double[] rtcCenter, Tile tile)
        {
            //get the transformationMAtrix from the gameObject created bij GltFast
            Matrix4x4 BasisMatrix = Matrix4x4.TRS(scene.position, scene.rotation, scene.localScale);
            TileTransform basistransform = new TileTransform()
            {
                m00 = BasisMatrix.m00,
                m01 = BasisMatrix.m01,
                m02 = BasisMatrix.m02,
                m03 = BasisMatrix.m03,

                m10 = BasisMatrix.m10,
                m11 = BasisMatrix.m11,
                m12 = BasisMatrix.m12,
                m13 = BasisMatrix.m13,

                m20 = BasisMatrix.m20,
                m21 = BasisMatrix.m21,
                m22 = BasisMatrix.m22,
                m23 = BasisMatrix.m23,

                m30 = BasisMatrix.m30,
                m31 = BasisMatrix.m31,
                m32 = BasisMatrix.m32,
                m33 = BasisMatrix.m33,
            };

            // transformation from created gameObject back to GLTF-space
            // this transformation has to be changed when moving to Unith.GLTFast version 4.0 or older (should then be changed to m00=1,m11=1,m22=-1,m33=1)
            TileTransform gltFastToGLTF = new TileTransform()
            {
                m00 = -1d,
                m11 = 1,
                m22 = 1,
                m33 = 1,
            };

            //transformation y-up to Z-up, to change form gltf-space to 3dtile-space
            TileTransform yUpToZUp = new TileTransform()
            {
                m00 = 1d,
                m12 = -1d,
                m21 = 1,
                m33 = 1d
            };

            //get the transformation of the created gameObject in 3dTile-space
            TileTransform geometryInECEF = yUpToZUp*gltFastToGLTF * basistransform;

            //apply the tileTransform
            TileTransform geometryInCRS = tile.tileTransform * geometryInECEF;

            //transformation from ECEF to Unity
            TileTransform ECEFToUnity = new TileTransform() //from ecef to Unity
            {
                m01 = 1d,   //unityX = ecefY
                m12 = 1d,   //unity = ecefZ
                m20 = -1d,  //unityZ = ecef-x
                m33=1d
            };

            // move the transformation to Unity-space
            TileTransform geometryInUnity = ECEFToUnity * geometryInCRS;

            // create a transformation using floats to be able to extract scale and rotation in unity-space
            Matrix4x4 final = new Matrix4x4()
            {
                m00 = (float)geometryInUnity.m00,
                m01 = (float)geometryInUnity.m01,
                m02 = (float)geometryInUnity.m02,
                m03 = 0f,

                m10 = (float)geometryInUnity.m10,
                m11 = (float)geometryInUnity.m11,
                m12 = (float)geometryInUnity.m12,
                m13 = 0f,

                m20 = (float)geometryInUnity.m20,
                m21 = (float)geometryInUnity.m21,
                m22 = (float)geometryInUnity.m22,
                m23 = 0f,

                m30 = 0f,
                m31=0f,
                m32=0f,
                m33=1f
            };
            Vector3 translation;
            Vector3 scale;
            Quaternion rotation;
            // get rotation and scale in unity-space
            final.Decompose(out translation, out rotation, out scale);

            // get the coordinate of the origin of the created gameobject in the 3d-tiles CoordinateSystem
            Coordinate sceneCoordinate = new Coordinate(tile.content.contentcoordinateSystem, geometryInCRS.m03, geometryInCRS.m13, geometryInCRS.m23);
            if (rtcCenter != null)
            {
                sceneCoordinate = new Coordinate(tile.content.contentcoordinateSystem, rtcCenter[0], rtcCenter[1], rtcCenter[2])+sceneCoordinate;
            }

            /// TEMPORARY FIX
            /// rotationToGRavityUp applies an extra rotation of -90 degrees around the up-axis in case of ECEF-coordinateSystems. 
            /// dis should not be done and has to be removed
            /// until that time, we rotate by 90 degrees around the up-axis to counter the applied rotation
            rotation = Quaternion.AngleAxis(90, Vector3.up) * rotation;
            tile.content.contentCoordinate = sceneCoordinate;
            
            //apply scale, position and rotation to the gameobject
            scene.localScale = scale;
            ScenePosition scenepos = scene.gameObject.AddComponent<ScenePosition>();
            scenepos.contentposition = sceneCoordinate;
            scene.position = sceneCoordinate.ToUnity();
            scene.rotation = sceneCoordinate.RotationToLocalGravityUp() * rotation;
            

        }
        public void ParseAssetMetaData(Content content)
        {
            string gltfJsonText = null;
            
            if (glbBuffer != null)
            {
                // Extract JSON from GLB binary format
                var gltfAndBin = ExtractJsonAndBinary(glbBuffer);
                gltfJsonText = gltfAndBin.Item1;
            }
            else if (gltfJsonData != null)
            {
                // Convert byte array to string for standalone GLTF files
                gltfJsonText = System.Text.Encoding.UTF8.GetString(gltfJsonData);
            }
            else if (gltfImport != null)
            {
                // Try to get JSON from the GLTF import directly
                // This is for standalone GLTF files (not GLB)
                var sourceJson = gltfImport.GetSourceRoot()?.ToString();
                if (!string.IsNullOrEmpty(sourceJson))
                {
                    gltfJsonText = sourceJson;
                }
                else
                {
                    Debug.LogWarning("Could not get source JSON from GLTF import");
                    return;
                }
            }
            else
            {
                Debug.LogError("No GLTF data source available - cannot extract metadata");
                return;
            }

            if (string.IsNullOrEmpty(gltfJsonText))
            {
                Debug.LogError("Could not extract GLTF JSON text");
                return;
            }

            //Deserialize json using JSON.net instead of Unity's JsonUtility ( gave silent error )
            var gltfRoot = JsonConvert.DeserializeObject<GltfMeshFeatures.GltfRootObject>(gltfJsonText);
            
            var metadata = content.gameObject.AddComponent<ContentMetadata>();
            metadata.asset = gltfRoot.asset;

            content.tilesetReader.OnLoadAssetMetadata.Invoke(metadata);
        }

        /// <summary>
        /// Parse subobjects from gltf data
        /// </summary>
        /// <param name="parent">Parent transform where scenes were spawned in</param>
        public void ParseSubObjects(Transform parent)
        {
            //Extract json from glb
            var gltfAndBin = ExtractJsonAndBinary(glbBuffer);
            var gltfJsonText = gltfAndBin.Item1;
            var binaryBlob = gltfAndBin.Item2;

            //Deserialize json using JSON.net instead of Unity's JsonUtility ( gave silent error )
            var gltfFeatures = JsonConvert.DeserializeObject<GltfMeshFeatures.GltfRootObject>(gltfJsonText);

            var featureIdBufferViewIndex = 0;
            foreach (var mesh in gltfFeatures.meshes)
            {
                foreach (var primitive in mesh.primitives)
                {
                    featureIdBufferViewIndex = primitive.attributes._FEATURE_ID_0;
                }
            }
            if (featureIdBufferViewIndex == -1)
            {
                Debug.LogWarning("_FEATURE_ID_0 was not found in the dataset. This is required to find BAG id's.");
                return;
            }

            //Use feature ID as bufferView index and get bufferview
            var featureAccessor = gltfFeatures.accessors[featureIdBufferViewIndex];
            var targetBufferView = gltfFeatures.bufferViews[featureAccessor.bufferView];

            var compressed = false;

            var featureIdBuffer = GetFeatureBuffer(gltfFeatures.buffers, targetBufferView, binaryBlob, compressed);
            if (featureIdBuffer == null || featureIdBuffer.Length == 0)
            {
                Debug.LogWarning("Getting feature buffer failed.");
                return;
            }

            //Parse feature table into List<float>
            List<Vector2Int> vertexFeatureIds = new();
            var stride = targetBufferView.byteStride;
            int currentFeatureTableIndex = -1;
            int vertexCount = 0;
            int accessorOffset = featureAccessor.byteOffset;
            for (int i = 0; i < featureIdBuffer.Length; i += stride)
            {
                //TODO: Read componentType from accessor to determine how to read the featureTableIndex
                var featureTableIndex = (int)BitConverter.ToSingle(featureIdBuffer, i+accessorOffset); 
                
                if (currentFeatureTableIndex != featureTableIndex)
                {
                    if (currentFeatureTableIndex != -1)
                        vertexFeatureIds.Add(new Vector2Int(currentFeatureTableIndex, vertexCount));

                    currentFeatureTableIndex = featureTableIndex;
                    vertexCount = 1;
                }
                else
                {
                    vertexCount++;
                }
            }
            //Finish last feature table entry
            vertexFeatureIds.Add(new Vector2Int(currentFeatureTableIndex, vertexCount));

            //Retrieve EXT_structural_metadata tables
            var propertyTables = gltfFeatures.extensions.EXT_structural_metadata.propertyTables;

            //Now parse the property tables BAGID 
            var bagIdList = new List<string>();
            foreach (var propertyTable in propertyTables)
            {
                //Now parse the data from the buffer using stringOffsetType=UINT32
                var bagpandid = propertyTable.properties.bagpandid; //Based on Tyler dataset key naming
                var identificatie = propertyTable.properties.identificatie;  //Based on PG2B3DM dataset key naming

                var bufferViewIndex = (bagpandid != null) ? bagpandid.values : identificatie.values; //Values reference the bufferView index
                var count = propertyTable.count;
                var bufferView = gltfFeatures.bufferViews[bufferViewIndex];
                var stringSpan = bufferView.byteLength / count; //string length in bytes

                //Directly convert the buffer to a list of strings
                var stringBytesSpan = new Span<byte>(binaryBlob, (int)bufferView.byteOffset, bufferView.byteLength);
                for (int i = 0; i < count; i++)
                {
                    var stringBytesSpanSlice = stringBytesSpan.Slice(i * stringSpan, stringSpan);
                    var stringBytes = stringBytesSpanSlice.ToArray();
                    var stringBytesString = Encoding.ASCII.GetString(stringBytes);
                    bagIdList.Add(stringBytesString);
                }
                break; //Just support one for now.
            }

#if SUBOBJECT

            foreach (Transform child in parent)
            {
                Debug.Log(child.name,child.gameObject);
                //Add subobjects to the spawned gameobject
                child.gameObject.AddComponent<MeshCollider>();
                ObjectMapping objectMapping = child.gameObject.AddComponent<ObjectMapping>();
                objectMapping.items = new List<ObjectMappingItem>();

                //For each uniqueFeatureIds, add a subobject
                int offset = 0;
                for (int i = 0; i < vertexFeatureIds.Count; i++)
                {
                    var uniqueFeatureId = vertexFeatureIds[i];
                    var bagId = bagIdList[uniqueFeatureId.x];
                    
                    //Remove any prefixes/additions to the bag id
                    bagId = Regex.Replace(bagId, "[^0-9]", "");

                    var subObject = new ObjectMappingItem()
                    {
                        objectID = bagId,
                        firstVertex = offset,
                        verticesLength = uniqueFeatureId.y
                    };
                    objectMapping.items.Add(subObject);
                    offset += uniqueFeatureId.y;
                }
            }
            return;
#endif
            Debug.LogWarning("Subobjects are not supported in this build. Please use the Netherlands3D.SubObjects package.");
        }

        private byte[] GetFeatureBuffer(GltfMeshFeatures.Buffer[] buffers, GltfMeshFeatures.BufferView bufferView, byte[] glbBuffer, bool decompress)
        {
            //Because the mesh is compressed, we need to get the buffer and decompress it
            var byteLength = decompress ? bufferView.extensions.EXT_meshopt_compression.byteLength : bufferView.byteLength;
            var byteOffset = decompress ? bufferView.extensions.EXT_meshopt_compression.byteOffset : bufferView.byteOffset;

            //Create NativeArray from byte[] glbBuffer
            glbBufferNative = new NativeArray<byte>(glbBuffer, Allocator.Persistent);

            //Create NativeSlice as part of the glbBuffer that the view is covering
            source = glbBufferNative.Slice((int)byteOffset, (int)byteLength);
            if(!decompress)
            {
                //Convert slice to byte[] array
                Debug.Log("Use buffer directly");
                return source.ToArray();
            }
            Debug.Log("Decompress buffer");
            var byteStride = bufferView.extensions.EXT_meshopt_compression.byteStride;
            var count = bufferView.extensions.EXT_meshopt_compression.count;   

            //Create NativeArray to store the decompressed buffer
            destination = new NativeArray<byte>(count * byteStride, Allocator.Persistent);

            //Decompress using meshop decomression
            var success = Decode.DecodeGltfBufferSync(
                destination,
                count,
                byteStride,
                source,
                Meshoptimizer.Mode.Attributes,
                Meshoptimizer.Filter.None
            );

            return destination.ToArray();
        }

        public static (string, byte[]) ExtractJsonAndBinary(byte[] glbData)
        {
            if (glbData.Length < 12)
                Debug.Log("GLB file is too short.");

            //Check the magic bytes to ensure it's a GLB file
            var magicBytes = BitConverter.ToUInt32(glbData, 0);
            if (magicBytes != 0x46546C67) // "glTF"
                Debug.Log("Not a valid GLB file.");

            var version = BitConverter.ToUInt32(glbData, 4);
            var length = BitConverter.ToUInt32(glbData, 8);

            if (version != 2)
                Debug.Log("Unsupported GLB version.");

            if (glbData.Length != length)
                Debug.Log("GLB file length does not match the declared length.");

            //Find the JSON chunk
            var jsonChunkLength = BitConverter.ToUInt32(glbData, 12);
            if (jsonChunkLength == 0)
                Debug.Log("JSON chunk is missing.");
            var jsonChunkOffset = 20; //GLB header (12 bytes) + JSON chunk header (8 bytes)

            //Extract JSON as a string
            var json = Encoding.UTF8.GetString(glbData, jsonChunkOffset, (int)jsonChunkLength);

            // Find the binary chunk
            var binaryChunkLength = length - jsonChunkLength - 28; //28 = GLB header (12 bytes) + JSON chunk header (8 bytes) + JSON chunk length (4 bytes) + BIN chunk header (8 bytes)
            if (binaryChunkLength == 0)
                Debug.Log("BIN chunk is missing.");
            var binaryChunkOffset = jsonChunkOffset + (int)jsonChunkLength + 8; //JSON chunk header (8 bytes) + JSON chunk length (4 bytes)

            //Extract binary data as a byte array
            var binaryData = new byte[binaryChunkLength];
            System.Buffer.BlockCopy(glbData, binaryChunkOffset, binaryData, 0, (int)binaryChunkLength);

            return (json, binaryData);
        }

        public void OverrideAllMaterials(UnityEngine.Material overrideMaterial)
        {
            if (parentTransform == null)
            {
                return;
            }
            foreach (var renderer in parentTransform.GetComponentsInChildren<Renderer>())
            {
                renderer.material = overrideMaterial;
            }
        }
    }
}