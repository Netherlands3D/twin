using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Rendering
{
    public class PointRenderer3D : BatchedMeshInstanceRenderer2
    {
        protected override void GenerateTransformMatrixCache(int collectionStartIndex = -1)
        {
            var batchCount = (pointCount / 1023) + 1; //x batches of 1023 + 1 for the remainder

            if (collectionStartIndex < 0) //reset cache completely
            {
                pointTransformMatrixCache = new List<List<Matrix4x4>>(batchCount);
                collectionStartIndex = 0;
            }

            pointTransformMatrixCache.Capacity = batchCount;

            var flattenedStartIndex = GetFlattenedStartIndex(collectionStartIndex);
            var matrixIndices = GetMatrixIndices(flattenedStartIndex); //each point in the line is a joint

            for (var i = collectionStartIndex; i < positionCollections.Count; i++)
            {
                var collection = positionCollections[i];
                for (int j = 0; j < collection.Count; j++)
                {
                    var currentPoint = collection[j].ToUnity();

                    // Flatten the Y axis if needed
                    currentPoint.y = (FlattenY ? 0 : currentPoint.y) + offsetY;

                    // Create the joint using a sphere aligned with the cylinder (with matching faces for smooth transition between the two)
                    var scale = new Vector3(PointMeshScale, PointMeshScale, PointMeshScale);
                    Matrix4x4 jointTransformMatrix = Matrix4x4.TRS(currentPoint, Quaternion.identity, scale); //todo: add serialized rotation?
                    AppendMatrixToBatches(pointTransformMatrixCache, ref matrixIndices.batchIndex, ref matrixIndices.matrixIndex, jointTransformMatrix);
                }
            }
        }
    }
}