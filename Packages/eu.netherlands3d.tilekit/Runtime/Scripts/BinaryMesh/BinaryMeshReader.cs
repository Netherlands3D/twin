using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;

namespace Netherlands3D.Tilekit.BinaryMesh
{
    public class BinaryMeshReader
    {
        private struct SubMeshInfo
        {
            public int subMeshID;
            public int subMeshFirstIndex;
            public int subMeshIndexCount;
            public int submeshFirstVertex;
            public int submeshVertexCount;
        }
        
        public static (Mesh, int[]) FromData(byte[] fileBytes)
        {
            Profiler.BeginSample("ReadBinaryMeshUnSafe (Wytze)");
            
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

                    //submeshMaterialIndices = materialIndices;
                    
                    Profiler.EndSample();
                    
                    Profiler.EndSample();

                    return (mesh, materialIndices);
                }
                
                
            }
        }
        
        public static void FromByteArray<T>(byte[] source, ref int offset, ref T destination) where T : unmanaged
        {
            var destinationSpan = MemoryMarshal.CreateSpan(ref destination, 1);
            FromByteArray(source, ref offset, destinationSpan);
        }

        public static void FromByteArray<T>(byte[] source, ref int offset, T[] destination) where T : unmanaged
        {
            var destinationSpan = new Span<T>(destination);
            FromByteArray(source, ref offset, destinationSpan);
        }

        private static void FromByteArray<T>(byte[] source, ref int offset, Span<T> destination) where T : unmanaged
        {
            var destinationBytes = MemoryMarshal.AsBytes(destination);
            var length = destinationBytes.Length;
            
            var sourceBytes = new ReadOnlySpan<byte>(source, offset, length);
            
            sourceBytes.CopyTo(destinationBytes);
            
            offset += length;
        }
        
    }
    
    
    
}
