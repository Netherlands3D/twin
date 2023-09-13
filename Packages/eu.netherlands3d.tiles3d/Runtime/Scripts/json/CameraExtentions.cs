using UnityEngine;
using System.Collections;



public static class CameraExtensions
{
    public static Vector3[] corners = new Vector3[4];

    private static Vector3 unityMin;
    private static Vector3 unityMax;

    private static Vector2 topLeft = new Vector2(0, 1);
    private static Vector2 topRight = new Vector2(1, 1);
    private static Vector2 bottomRight = new Vector2(1, 0);
    private static Vector2 bottomLeft = new Vector2(0, 0);

    private static Plane[] cameraFrustumPlanes = new Plane[6]
	{
		new Plane(), //Left
		new Plane(), //Right
		new Plane(), //Down
		new Plane(), //Up
		new Plane(), //Near
		new Plane(), //Far
	};



    public static bool InView(this Camera camera, Bounds bounds)
    {
        GeometryUtility.CalculateFrustumPlanes(camera, cameraFrustumPlanes);
        return GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, bounds);
    }
}
