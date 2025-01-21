using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin.Configuration.TileHandler
{
    public class LoadRelativeCenter : MonoBehaviour
    {
        private void OnEnable()
        {
            ProjectData.Current.OnCameraPositionChanged.AddListener(Apply);
        }

        private void OnDisable()
        {
            ProjectData.Current.OnCameraPositionChanged.RemoveListener(Apply);
        }

        private void Apply(Coordinate coordinate)
        {
            GetComponent<Origin>().MoveOriginTo(coordinate);
        }
    }
}