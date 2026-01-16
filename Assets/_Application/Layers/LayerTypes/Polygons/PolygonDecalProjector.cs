using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Services;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.Samplers;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
{
    public class PolygonDecalProjector : MonoBehaviour
    {
        [Header("Mask camera settings")]
        [SerializeField] private float minDistance = 100;
        [SerializeField] private float maxDistance = 1000;

        [SerializeField] private float minCamHeightMultiplier = 0.1f;
        [SerializeField] private float maxCamHeightMultiplier = 100f;
        
        [Header("Projection settings")]
        [SerializeField] private AnimationCurve lookDirectionResolution;
        
        private DecalProjector decalProjector;
        public Camera ProjectionCamera { get; private set; }
        private Camera mainCamera;
        private HeightMap heightMap;
        
        private void Awake()
        {
            decalProjector = GetComponent<DecalProjector>();
            ProjectionCamera = GetComponent<Camera>();
            
        }

        private void Start()
        {
            heightMap = ServiceLocator.GetService<HeightMap>();
            CameraService cameraService = ServiceLocator.GetService<CameraService>();
            mainCamera = cameraService.ActiveCamera;
        }

        private void Update()
        {
            if(!mainCamera.isActiveAndEnabled) return;
            
            var lookingForward = 1-Math.Abs(Vector3.Dot(Vector3.down, mainCamera.transform.forward)); //0 is looking top down, 1 is looking straight to the horizon
            var sampleMaxDistance = Mathf.Lerp(maxDistance, minDistance, lookDirectionResolution.Evaluate(lookingForward));

            var minCamHeight = -50f;
            var maxCamHeight = 1500f;
            var camHeight = mainCamera.transform.position.y  - heightMap.GetHeight(new Coordinate(mainCamera.transform.position));
            var normalizedHeight = Mathf.InverseLerp(minCamHeight, maxCamHeight, camHeight);
            var camHeightMultiplier = Mathf.Lerp(minCamHeightMultiplier, maxCamHeightMultiplier, normalizedHeight);
            sampleMaxDistance *= camHeightMultiplier;
            
            var extent = mainCamera.GetExtent(sampleMaxDistance);
            var w = (float)extent.Width;
            var h = (float)extent.Height;
            var maxDimension = Mathf.Max(w,h);

            var pos = new Vector3((float)extent.CenterX, 500, (float)extent.CenterY);
            
            ProjectionCamera.transform.position = pos;
            ProjectionCamera.orthographicSize = maxDimension / 2;

            if (decalProjector)
            {
                var size = new Vector3(maxDimension, maxDimension, decalProjector.size.z);
                decalProjector.size = size;
            }
        }
    }
}