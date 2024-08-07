using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Netherlands3D.CartesianTiles;

namespace Netherlands3D.Twin
{
    public class SensorTileSelection : MonoBehaviour
    {
        private SensorProjectionLayer sensorProjectionLayer;
        private TileSensorDataController previousTile;
        private Plane hitPlane = new Plane();

        private void OnEnable()
        {
            ClickNothingPlane.ClickedOnNothing.AddListener(ProcessClick);
        }

        private void OnDisable()
        {
            ClickNothingPlane.ClickedOnNothing.RemoveListener(ProcessClick);
        }

        private void ProcessClick()
        {
            if (sensorProjectionLayer == null)
                sensorProjectionLayer = GetComponent<SensorProjectionLayer>();
            SensorDataController dataController = sensorProjectionLayer.SensorDataController;           

            var position = Pointer.current.position.ReadValue();
            var ray = Camera.main.ScreenPointToRay(position);
            hitPlane.SetNormalAndPosition(Vector3.up, transform.position);
            if (hitPlane.Raycast(ray, out var enterNow))
            {
                Vector3 hitPoint = ray.GetPoint(enterNow);
                Vector2Int key = dataController.GetTileKeyFromUnityPosition(hitPoint, sensorProjectionLayer.tileSize);
                TileSensorDataController tileController = sensorProjectionLayer.GetTileController(key);
                if (tileController == null)
                    return;

                tileController.ActivateHexagon(hitPoint, sensorProjectionLayer.tileSize, dataController);
                if (previousTile != null && previousTile != tileController)
                    previousTile.DeactivateHexagon();
                previousTile = tileController;                
            }
        }        
    }
}