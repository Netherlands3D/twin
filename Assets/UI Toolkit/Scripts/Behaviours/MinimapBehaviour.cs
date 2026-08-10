using Netherlands3D.Coordinates;
using Netherlands3D.Minimap;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D
{
    public class MinimapBehaviour : MonoBehaviour
    {
        [SerializeField] private MinimapConfig minimapConfig;
        private MapViewport minimap;
        private Camera activeCamera;
        
        void Start()
        {
            minimap = App.UIRoot.Root.Q<MapViewport>();
            minimap.Initialize(minimapConfig);
            
            minimap.CoordinateMoveRequested.AddListener(MoveCameraToCoordinate);
            
            var cameraService = ServiceLocator.GetService<CameraService>();
            activeCamera = cameraService.ActiveCamera;
            OnCameraPositionChanged(activeCamera.transform.position);
            cameraService.OnPositionChanged.AddListener(OnCameraPositionChanged);
            cameraService.OnSwitchCamera.AddListener(OnCameraSwitch);
        }

        private void MoveCameraToCoordinate(Coordinate newCoordinate)
        {
            if (!newCoordinate.IsValid()) return;
            
            var coord = newCoordinate.Convert(CoordinateSystem.RDNAP);
            coord.height = activeCamera.transform.position.y;
            if (!activeCamera.TryGetComponent<WorldTransform>(out var worldTransform))
                return;

            worldTransform.MoveToCoordinate(coord);
        }

        private void OnCameraPositionChanged(Vector3 newWorldPosition)
        {
            var coordinate = new Coordinate(newWorldPosition).Convert(CoordinateSystem.RDNAP);
            minimap.SetLocation(coordinate);
        }
        
        private void OnCameraSwitch(Camera newCamera)
        {
            activeCamera = newCamera;
            OnCameraPositionChanged(activeCamera.transform.position);
        }
    }
}
