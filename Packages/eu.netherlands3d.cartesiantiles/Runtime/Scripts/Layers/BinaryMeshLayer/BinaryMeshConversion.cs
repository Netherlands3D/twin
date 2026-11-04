using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
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
        

        public static Mesh ReadBinaryMesh(NativeArray<byte>.ReadOnly fileBytes, out int[] submeshMaterialIndices)
        {
#if !SAFE_READING_BINARY_TILE_DATA
            return ReadBinaryMeshUnSafe(fileBytes, out submeshMaterialIndices);
#endif
        }

        private static int ReadInt32(byte[] source, ref int offset)
        {
            var value = BinaryPrimitives.ReadInt32LittleEndian(source.AsSpan(offset, sizeof(int)));
            offset += sizeof(int);
            return value;
        }

        private static int binaryMeshUnsafePicker = 0;
        

        public static Mesh ReadBinaryMeshUnSafe(NativeArray<byte>.ReadOnly fileBytes, out int[] submeshMaterialIndices)
        {
            
            Profiler.BeginSample("ReadBinaryMeshUnSafe");
            
            // todo: array reuse through BufferedStream bs;?

            //using (var stream = new MemoryStream(fileBytes))
            {

                //using (BinaryReader reader = new BinaryReader(stream))
                {
                    
                    Profiler.BeginSample("Read index data");
                    
                    var offset = 0;
                    
                    var version = 0;
                    var vertexCount = 0;
                    var normalsCount = 0;
                    var uvsCount = 0;
                    var indicesCount = 0;
                    var submeshCount = 0;
                    
                    FromByteArray(fileBytes, ref offset, ref version);
                    FromByteArray(fileBytes, ref offset, ref vertexCount);
                    FromByteArray(fileBytes, ref offset, ref normalsCount);
                    FromByteArray(fileBytes, ref offset, ref uvsCount);
                    FromByteArray(fileBytes, ref offset, ref indicesCount);
                    FromByteArray(fileBytes, ref offset, ref submeshCount);

                    Profiler.EndSample();
                    
                    Profiler.BeginSample("Create mesh");

                    var mesh = new Mesh();
                    
                    Profiler.EndSample();
                    
                    Profiler.BeginSample("Create vertices");
                    
                    Vector3[] vertices = new Vector3[vertexCount];
                    FromByteArray(fileBytes, ref offset, vertices);
                    mesh.vertices = vertices;
                    
                    Profiler.EndSample();
                    
                    Profiler.BeginSample("Create normals");

                    // Normals should be same size as vertices - right?
                    if (vertexCount != normalsCount)
                    {
                        // Otherwise resize:
                        vertices = new Vector3[normalsCount];
                    }

                    // Normals:
                    FromByteArray<Vector3>(fileBytes, ref offset, vertices);
                    mesh.normals = vertices;
                    
                    Profiler.EndSample();
                    
                    Profiler.BeginSample("Create UVs");

                    // UVS - if present.
                    if (uvsCount > 0)
                    {
                        Vector2[] uvs = new Vector2[uvsCount];
                        FromByteArray<Vector2>(fileBytes, ref offset, uvs);
                        mesh.uv = uvs;
                    }
                    
                    Profiler.EndSample();
                    
                    Profiler.BeginSample("Create indices");

                    // Indices:
                    int[] indices = new int[indicesCount];
                    FromByteArray<int>(fileBytes, ref offset, indices);

                    mesh.SetIndexBufferParams(indicesCount, UnityEngine.Rendering.IndexFormat.UInt32);
                    mesh.SetIndexBufferData(indices, 0, 0, indicesCount, UnityEngine.Rendering.MeshUpdateFlags.DontNotifyMeshUsers | UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds | UnityEngine.Rendering.MeshUpdateFlags.DontResetBoneBounds | UnityEngine.Rendering.MeshUpdateFlags.DontValidateIndices);
                    
                    Profiler.EndSample();
                    
                    Profiler.BeginSample("Create submeshes");
                    
                    mesh.subMeshCount = submeshCount;

                    // Read submesh info:
                    SubMeshInfo[] subMeshInfos = new SubMeshInfo[submeshCount];
                    FromByteArray<SubMeshInfo>(fileBytes, ref offset, subMeshInfos);

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
                    
                    Profiler.EndSample();
                    
                    Profiler.EndSample();

                    return mesh;
                }
            }


        }

        

        public static void FromByteArray<T>(NativeArray<byte>.ReadOnly source, ref int offset, ref T destination) where T : unmanaged
        {
            var destinationSpan = MemoryMarshal.CreateSpan(ref destination, 1);
            FromByteArray(source, ref offset, destinationSpan);
        }

        public static void FromByteArray<T>(NativeArray<byte>.ReadOnly source, ref int offset, T[] destination) where T : unmanaged
        {
            var destinationSpan = new Span<T>(destination);
            FromByteArray(source, ref offset, destinationSpan);
        }

        private static void FromByteArray<T>(NativeArray<byte>.ReadOnly source, ref int offset, Span<T> destination) where T : unmanaged
        {
            var destinationBytes = MemoryMarshal.AsBytes(destination);
            var length = destinationBytes.Length;
            
            var sourceBytes = source.AsReadOnlySpan().Slice(offset, length);
            
            
            sourceBytes.CopyTo(destinationBytes);
            
            offset += length;
        }

        /*
         //this is the OG
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
        */

    }
}