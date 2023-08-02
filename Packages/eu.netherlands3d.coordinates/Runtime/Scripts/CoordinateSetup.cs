using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Coordinates
{
public class CoordinateSetup : MonoBehaviour
{
        public Vector2RD rdCoordinatesAtUnityCenter;
        public float napElevationAtUniytZero;
    // Start is called before the first frame update
    void Awake()
    {
            EPSG7415.relativeCenter = rdCoordinatesAtUnityCenter;
            EPSG7415.zeroGroundLevelY = napElevationAtUniytZero;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
}
