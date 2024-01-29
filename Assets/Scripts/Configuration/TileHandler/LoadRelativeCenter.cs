using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin.Configuration.TileHandler
{
    public class LoadRelativeCenter : MonoBehaviour
    {
        [SerializeField] private Configurator configurator;

        private void OnEnable()
        {
            configurator.Configuration.OnOriginChanged.AddListener(Apply);
        }

        private void OnDisable()
        {
            configurator.Configuration.OnOriginChanged.RemoveListener(Apply);
        }

        private void Apply(Coordinate coordinate)
        {
            var newCoordinate = CoordinateConverter.ConvertTo(coordinate, CoordinateSystem.RD);
            // CoordinateConverter.zeroGroundLevelY = ConfigurationFile.zeroGroundLevelY;
            EPSG7415.relativeCenter = new Vector2RD(newCoordinate.Points[0], newCoordinate.Points[1]);
        }
    }
}