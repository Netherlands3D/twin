using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
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
            // GetComponent<Origin>().MoveOriginTo(coordinate);
        }
    }
}