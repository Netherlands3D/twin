using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D.Twin
{
    public class PolygonDecalProjector : MonoBehaviour
    {
        [Header("Mask camera settings")]
        [SerializeField] private float minDistance = 100;
        [SerializeField] private float maxDistance = 1000;
        
        [Header("Mask texture settings")]
        [SerializeField] private AnimationCurve lookDirectionResolution;
        
        private DecalProjector decalProjector;
        private Camera projectionCamera;
        private OpticalRaycaster opticalRaycaster;

        private void Awake()
        {
            decalProjector = GetComponent<DecalProjector>();
            projectionCamera = GetComponent<Camera>();
            opticalRaycaster = FindAnyObjectByType<OpticalRaycaster>();
            // renderTexture = new RenderTexture(1024, 1024, 24);
        }

        private void Update()
        {
            var lookingForward = 1-Math.Abs(Vector3.Dot(Vector3.down, Camera.main.transform.forward)); //0 is looking top down, 1 is looking straight to the horizon
            var sampleMaxDistance = Mathf.Lerp(maxDistance, minDistance, lookDirectionResolution.Evaluate(lookingForward));
            print("sample: "+sampleMaxDistance);

            var minCamHeight = -20f;
            var maxCamHeight = 500f;
            var camHeight = Camera.main.transform.position.y; //todo: get real height above terrain: get bounds of all terrein, get terrain with smallest y extents. use this bounds center as an estimation
            var normalizedHeight = Mathf.InverseLerp(minCamHeight, maxCamHeight, camHeight);
            var camHeigtMultiplier = Mathf.Lerp(1, 50, normalizedHeight);
            sampleMaxDistance *= camHeigtMultiplier;
            // print("sample wit height: "+sampleMaxDistance);

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
    }
}