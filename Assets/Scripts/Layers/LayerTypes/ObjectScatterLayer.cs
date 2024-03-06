using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.UI.LayerInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class ObjectScatterLayer : ReferencedLayer
    {
        private Mesh mesh;
        private Material material;
        private ScatterGenerationSettings settings;
        private Matrix4x4[][] matrixBatches; //Graphics.DrawMeshInstanced can only draw 1023 instances at once
        private PolygonSelectionLayer polygonLayer => ReferencedProxy.ParentLayer as PolygonSelectionLayer;

        public override bool IsActiveInScene
        {
            get => gameObject.activeSelf;
            set
            {
                gameObject.SetActive(value);
                ReferencedProxy.UI.MarkLayerUIAsDirty();
            }
        }

        public void Initialize(PolygonSelectionLayer polygon, Mesh mesh, Material material)
        {
            this.mesh = mesh;
            this.material = material;
            settings = ScriptableObject.CreateInstance<ScatterGenerationSettings>();
            settings.SettingsChanged.AddListener(RecalculateScatterMatrices);

            StartCoroutine(InitializeAfterReferencedProxy(polygon));
        }

        private IEnumerator InitializeAfterReferencedProxy(PolygonSelectionLayer polygon)
        {
            yield return null; //wait for ReferencedProxy layer to be initialized
            ReferencedProxy.SetParent(polygon);
            RecalculateScatterMatrices();
            polygon.polygonChanged.AddListener(RecalculateScatterMatrices);
        }

        protected override void OnDestroy()
        {
            settings.SettingsChanged.RemoveListener(RecalculateScatterMatrices);
            polygonLayer.polygonChanged.RemoveListener(RecalculateScatterMatrices);
        }

        private void RecalculateScatterMatrices()
        {
            var scatterPoints = CompoundPolygon.GenerateScatterPoints(polygonLayer.Polygon, settings.Density, settings.Scatter, settings.Angle);
            var batchCount = (scatterPoints.Count / 1023) + 1; //x batches of 1023 + 1 for the remainder
            var remainder = scatterPoints.Count % 1023;

            print( scatterPoints.Count + " points in " + (batchCount - 1) + " batches of 1023 and a remainder of " + remainder);
            matrixBatches = new Matrix4x4[batchCount][];
            for (int i = 0; i < batchCount; i++)
            {
                var arraySize = i == batchCount - 1 ? remainder : 1023;
                matrixBatches[i] = new Matrix4x4[arraySize];
                for (int j = 0; j < arraySize; j++)
                {
                    var pos = new Vector3(scatterPoints[1023 * i + j].x, 10, scatterPoints[1023 * i + j].y); //todo: use optical raycaster to determine y of entire polygon
                    var rot = Quaternion.identity;
                    var scale = settings.GenerateRandomScale();
                    matrixBatches[i][j] = Matrix4x4.TRS(pos, rot, scale);
                }
            }
        }

        private void Update()
        {
            RenderBatches();
        }

        private void RenderBatches()
        {
            if (matrixBatches == null) //this happens in the first frame because the polygon is not yet initialized
                return;

            foreach (var matrixBatch in matrixBatches)
            {
                Graphics.DrawMeshInstanced(mesh, 0, material, matrixBatch);
            }
        }
    }
}