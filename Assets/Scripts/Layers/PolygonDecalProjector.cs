using System;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D.Twin
{
    public class PolygonDecalProjector : MonoBehaviour
    {
        [Header("Mask camera settings")]
        [SerializeField] private float minDistance = 100;
        [SerializeField] private float maxDistance = 1000;

        [SerializeField] private float minCamHeightMultiplier = 0.1f;
        [SerializeField] private float maxCamHeightMultiplier = 100f;
        
        [Header("Mask texture settings")]
        [SerializeField] private AnimationCurve lookDirectionResolution;
        
        private DecalProjector decalProjector;
        private Camera projectionCamera;
        [SerializeField] private CartesianTileLayer terrainLayer;

        private void Awake()
        {
            decalProjector = GetComponent<DecalProjector>();
            projectionCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            var lookingForward = 1-Math.Abs(Vector3.Dot(Vector3.down, Camera.main.transform.forward)); //0 is looking top down, 1 is looking straight to the horizon
            var sampleMaxDistance = Mathf.Lerp(maxDistance, minDistance, lookDirectionResolution.Evaluate(lookingForward));

            var minCamHeight = -50f;
            var maxCamHeight = 1500f;
            var camHeight = EstimateCameraHeight(); 
            var normalizedHeight = Mathf.InverseLerp(minCamHeight, maxCamHeight, camHeight);
            var t = lookDirectionResolution.Evaluate(normalizedHeight);
            var camHeightMultiplier = Mathf.Lerp(minCamHeightMultiplier, maxCamHeightMultiplier, normalizedHeight);
            sampleMaxDistance *= camHeightMultiplier;
            
            var extent = Camera.main.GetExtent(sampleMaxDistance);
            var w = (float)extent.Width;
            var h = (float)extent.Height;
            var maxDimension = Mathf.Max(w,h);

            var pos = new Vector3((float)extent.CenterX, 500, (float)extent.CenterY);
            var size = new Vector3(maxDimension, maxDimension, decalProjector.size.z);

            decalProjector.transform.position = pos;
            decalProjector.size = size;
            
            projectionCamera.orthographicSize = maxDimension / 2;
        }

        private float EstimateCameraHeight()
        {
            if (!terrainLayer)
                return Camera.main.transform.position.y;
            
            //estimate real height above terrain: get bounds of all terrein, get terrain with smallest y extents. use this bounds center as an estimation
            var tiles = terrainLayer.GetComponentsInChildren<MeshFilter>();
            var smallestYExtents = float.MaxValue;
            var estimatedTerrainHeight = 0f;

            foreach (var tile in tiles)
            {
                var tileBounds = tile.mesh.bounds;
                var ySize = tileBounds.size.y;
                if (ySize < smallestYExtents)
                {
                    smallestYExtents = ySize;
                    estimatedTerrainHeight = tileBounds.center.y;
                }
            }
            
            return Camera.main.transform.position.y - estimatedTerrainHeight; 
        }
    }
}