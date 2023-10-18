using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;

namespace Netherlands3D.CartesianTiles
{
    public class BinaryMeshConversion : MonoBehaviour
    {
        public struct SubMeshInfo
        {
            public int subMeshID;
            public int subMeshFirstIndex;
            public int subMeshIndexCount;
            public int submeshFirstVertex;
            public int submeshVertexCount;
        }

        public struct BinaryMeshTile
        {
            public int version;
            public int vertexCount;
            public int normalsCount;
            public int uvsCount;
            public int indicesCount;
            public int submeshCount;
        }


        private const int version = 1;



        

        [ContextMenu("Load from binary")]
        private void LoadFromBinary()
        {
            var meshFilter = GetComponent<MeshFilter>();
            Destroy(meshFilter);

            byte[] readBytes = File.ReadAllBytes(Application.persistentDataPath + "/mesh.bin");
            Profiler.BeginSample("readbinarymesh", this);

            gameObject.AddComponent<MeshFilter>().mesh = ReadBinaryMesh(readBytes, out int[] materialIndices);
        }

      



        public static Mesh ReadBinaryMesh(byte[] fileBytes, out int[] submeshMaterialIndices)
        {
#if !SAFE_READING_BINARY_TILE_DATA
            return ReadBinaryMeshUnSafe(fileBytes, out submeshMaterialIndices);
#endif

            using (var stream = new MemoryStream(fileBytes))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    var version = reader.ReadInt32();
                    var vertexCount = reader.ReadInt32();
                    var normalsCount = reader.ReadInt32();
                    var uvsCount = reader.ReadInt32();
                    var indicesCount = reader.ReadInt32();
                    var submeshCount = reader.ReadInt32();

                    var mesh = new Mesh();

                    Vector3[] vertices = new Vector3[vertexCount];
                    for (int i = 0; i < vertexCount; i++)
                    {
                        Vector3 vertex = new Vector3(
                            reader.ReadSingle(),
                            reader.ReadSingle(),
                            reader.ReadSingle()
                         );
                        vertices[i] = vertex;
                    }
                    mesh.vertices = vertices;

                    Vector3[] normals = new Vector3[normalsCount];
                    for (int i = 0; i < normalsCount; i++)
                    {
                        Vector3 normal = new Vector3(
                            reader.ReadSingle(),
                            reader.ReadSingle(),
                            reader.ReadSingle()
                         );
                        normals[i] = normal;
                    }
                    mesh.normals = normals;

                    Vector2[] uvs = new Vector2[uvsCount];
                    for (int i = 0; i < uvsCount; i++)
                    {
                        Vector2 uv = new Vector2(
                            reader.ReadSingle(),
                            reader.ReadSingle()
                         );
                        uvs[i] = uv;
                    }
                    mesh.uv = uvs;

                    int[] indices = new int[indicesCount];
                    for (int i = 0; i < indicesCount; i++)
                    {
                        var index = reader.ReadInt32();
                        indices[i] = index;
                    }

                    mesh.SetIndexBufferParams(indicesCount, UnityEngine.Rendering.IndexFormat.UInt32);
                    mesh.SetIndexBufferData(indices, 0, 0, indicesCount);

                    mesh.subMeshCount = submeshCount;
                    int[] materialIndices = new int[submeshCount];

                    for (int i = 0; i < submeshCount; i++)
                    {
                        var subMeshID = reader.ReadInt32();
                        materialIndices[i] = subMeshID;

                        var subMeshFirstIndex = reader.ReadInt32();
                        var subMeshIndexCount = reader.ReadInt32();
                        var submeshFirstVertex = reader.ReadInt32();
                        var submeshVertexCount = reader.ReadInt32();

                        var subMeshDescriptor = new UnityEngine.Rendering.SubMeshDescriptor();
                        subMeshDescriptor.baseVertex = 0;
                        subMeshDescriptor.firstVertex = submeshFirstVertex;
                        subMeshDescriptor.vertexCount = submeshVertexCount;

                        subMeshDescriptor.indexStart = subMeshFirstIndex;
                        subMeshDescriptor.indexCount = subMeshIndexCount;

                        mesh.SetSubMesh(i, subMeshDescriptor);
                    }
                    submeshMaterialIndices = materialIndices;

                    return mesh;
                }
            }
        }




        public static Mesh ReadBinaryMeshUnSafe(byte[] fileBytes, out int[] submeshMaterialIndices)
        {
            // todo: array reuse through BufferedStream bs;?

            using (var stream = new MemoryStream(fileBytes))
            {

                using (BinaryReader reader = new BinaryReader(stream))
                {
                    var version = reader.ReadInt32();
                    var vertexCount = reader.ReadInt32();
                    var normalsCount = reader.ReadInt32();
                    var uvsCount = reader.ReadInt32();
                    var indicesCount = reader.ReadInt32();
                    var submeshCount = reader.ReadInt32();

                    var mesh = new Mesh();

                    //byte[] b = new byte[Marshal.SizeOf<Vector3>() * vertexCount];
                    byte[] b = reader.ReadBytes(Marshal.SizeOf<Vector3>() * vertexCount);
                    Vector3[] vertices = new Vector3[vertexCount];
                    FromByteArray<Vector3>(b, vertices);
                    mesh.vertices = vertices;

                    // Normals should be same size as vertices - right?
                    if (vertexCount != normalsCount)
                    {
                        // Otherwise resize:
                        vertices = new Vector3[normalsCount];
                    }

                    // Normals:
                    b = reader.ReadBytes(Marshal.SizeOf<Vector3>() * normalsCount);
                    FromByteArray<Vector3>(b, vertices);
                    mesh.normals = vertices;

                    // UVS - if present.
                    if (uvsCount > 0)
                    {
                        Vector2[] uvs = new Vector2[uvsCount];
                        b = reader.ReadBytes(Marshal.SizeOf<Vector2>() * uvsCount);
                        FromByteArray<Vector2>(b, uvs);
                        mesh.uv = uvs;
                    }

                    // Indices:
                    int[] indices = new int[indicesCount];
                    b = reader.ReadBytes(sizeof(int) * indicesCount);
                    FromByteArray<int>(b, indices);

                    mesh.SetIndexBufferParams(indicesCount, UnityEngine.Rendering.IndexFormat.UInt32);
                    mesh.SetIndexBufferData(indices, 0, 0, indicesCount, UnityEngine.Rendering.MeshUpdateFlags.DontNotifyMeshUsers | UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds | UnityEngine.Rendering.MeshUpdateFlags.DontResetBoneBounds | UnityEngine.Rendering.MeshUpdateFlags.DontValidateIndices);
                    mesh.subMeshCount = submeshCount;

                    // Read submesh info:
                    SubMeshInfo[] subMeshInfos = new SubMeshInfo[submeshCount];
                    b = reader.ReadBytes(Marshal.SizeOf<SubMeshInfo>() * submeshCount);
                    FromByteArray<SubMeshInfo>(b, subMeshInfos);

                    int[] materialIndices = new int[submeshCount];

                    for (int i = 0; i < submeshCount; i++)
                    {
                        materialIndices[i] = subMeshInfos[i].subMeshID;

                        var subMeshDescriptor = new UnityEngine.Rendering.SubMeshDescriptor()
                        {
                            baseVertex = 0,
                            firstVertex = subMeshInfos[i].submeshFirstVertex,
                            vertexCount = subMeshInfos[i].submeshVertexCount,

                            indexStart = subMeshInfos[i].subMeshFirstIndex,
                            indexCount = subMeshInfos[i].subMeshIndexCount,
                        };

                        mesh.SetSubMesh(i, subMeshDescriptor);
                    }

                    submeshMaterialIndices = materialIndices;

                    return mesh;
                }
            }

        }


        public static void FromByteArray<T>(byte[] source, T[] destination) where T : struct
        {
            //  T[] destination = new T[source.Length / Marshal.SizeOf(typeof(T))];
            GCHandle handle = GCHandle.Alloc(destination, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                Marshal.Copy(source, 0, pointer, source.Length);
                // return destination;
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
        }

        public static void FromByteArray<T>(byte[] source, T[] destination, int sourceLength) where T : struct
        {
            //  T[] destination = new T[source.Length / Marshal.SizeOf(typeof(T))];
            GCHandle handle = GCHandle.Alloc(destination, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                Marshal.Copy(source, 0, pointer, sourceLength);
                // return destination;
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
        }


    }
}