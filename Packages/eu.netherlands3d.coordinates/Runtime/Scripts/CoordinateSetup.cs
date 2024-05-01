using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Coordinates
{
public class CoordinateSetup : MonoBehaviour
{
        public CoordinateSystem coordintesystem;
        public Vector2RD rdCoordinatesAtUnityCenter;
        public float napElevationAtUniytZero;
    // Start is called before the first frame update
    void Awake()
    {
            CoordinateSystems.connectedCoordinateSystem = coordintesystem;
            CoordinateSystems.SetOrigin(new Coordinate(CoordinateSystem.RDNAP, rdCoordinatesAtUnityCenter.x, rdCoordinatesAtUnityCenter.y, napElevationAtUniytZero));
            CoordinateConverter.relativeCenterRD = rdCoordinatesAtUnityCenter;
            CoordinateConverter.zeroGroundLevelY = napElevationAtUniytZero;

    }
}
}
