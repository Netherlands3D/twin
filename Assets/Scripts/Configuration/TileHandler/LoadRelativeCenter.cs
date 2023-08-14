using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin.Configuration.TileHandler
{
    public class LoadRelativeCenter : MonoBehaviour
    {
        [SerializeField] private Configurator configurator;

        private void OnEnable()
        {
            configurator.OnLoaded.AddListener(Apply);
        }

        private void OnDisable()
        {
            configurator.OnLoaded.RemoveListener(Apply);
        }

        private void Apply(Configuration configuration)
        {
            var rdCoordinates = CoordinateConverter.ConvertTo(configuration.Origin, CoordinateSystem.RD);
            // CoordinateConverter.zeroGroundLevelY = ConfigurationFile.zeroGroundLevelY;
            EPSG7415.relativeCenter = new Vector2RD(rdCoordinates.Points[0], rdCoordinates.Points[1]);
        }
    }
}