using UnityEngine;
using UnityEngine.InputSystem;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Twin;

namespace Netherlands3D.Functionalities.UrbanReLeaf
{
    public class SensorTileSelection : MonoBehaviour
    {
        private SensorProjectionLayer sensorProjectionLayer;
        private TileSensorDataController previousTile;
        private Plane hitPlane = new Plane();
        private static SensorProjectionLayer currentSelectingLayer = null;

        private void OnEnable()
        {
            if (sensorProjectionLayer == null)
                sensorProjectionLayer = GetComponent<SensorProjectionLayer>();
            sensorProjectionLayer.onLayerDisabled.AddListener(Deactivate);
            ClickNothingPlane.ClickedOnNothing.AddListener(ProcessClick);
        }

        private void OnDisable()
        {
            sensorProjectionLayer.onLayerDisabled.RemoveListener(Deactivate);
            ClickNothingPlane.ClickedOnNothing.RemoveListener(ProcessClick);
        }

        private void Deactivate()
        {
            if(sensorProjectionLayer == currentSelectingLayer)
                currentSelectingLayer = null;
            if (previousTile != null)
                previousTile.DestroySelectedHexagon();
        }

        private void ProcessClick()
        {
            //lets check if not multiple layers are selecting hexagons
            if (currentSelectingLayer != null && sensorProjectionLayer != currentSelectingLayer)
                return;

            if(currentSelectingLayer != null && sensorProjectionLayer == currentSelectingLayer && !currentSelectingLayer.isEnabled)
            {               
                Deactivate();
                return;
            }
            
            //this is now the current selecting layer
            currentSelectingLayer = sensorProjectionLayer;

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