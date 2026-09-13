using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Rendering
{
    public class PointRenderer3D : BatchedMeshInstanceRenderer
    {
        public override Material[] Materials => new Material[] { PointMaterial };

        /// <summary>
        /// Applies one color per position collection. GeoJSON point features normally contribute one collection,
        /// while MultiPoints can keep all of their points on the same feature color.
        /// </summary>
        public void SetCollectionColors(IReadOnlyList<Color> colors)
        {
            UpdateColorBuffers();

            foreach (var batch in pointBatchColors)
                System.Array.Fill(batch.Colors, PointMaterial.color);

            var pointIndex = 0;
            for (var collectionIndex = 0; collectionIndex < positionCollections.Count; collectionIndex++)
            {
                var collection = positionCollections[collectionIndex];
                var color = collectionIndex < colors.Count ? colors[collectionIndex] : PointMaterial.color;

                for (var point = 0; point < collection.Count; point++)
                {
                    var batchIndex = pointIndex / 1023;
                    var matrixIndex = pointIndex % 1023;
                    if (batchIndex < pointBatchColors.Count)
                        pointBatchColors[batchIndex].Colors[matrixIndex] = color;
                    pointIndex++;
                }
            }

            foreach (var batch in pointBatchColors)
                batch.MaterialPropertyBlock.SetVectorArray("_SegmentColors", batch.Colors);
        }

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
