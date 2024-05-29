using System.Collections;
using System.Globalization;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Configuration
{
    public class ConfiguratorStarter : MonoBehaviour
    {
        [SerializeField] private Configurator configurator;
        public UnityEvent<Configuration> OnLoadedConfiguration = new();

        /// <summary>
        /// Make sure to set the culture to invariant to prevent issues with parsing floats and doubles.
        /// This way we consistently use the dot as the decimal separator.
        /// </summary>
        private void Awake() {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Coordinates.CoordinateSystems.connectedCoordinateSystem = Coordinates.CoordinateSystem.RDNAP;
            Coordinates.CoordinateSystems.SetOrigin(new Coordinates.Coordinate(Coordinates.CoordinateSystem.RDNAP, 120000, 480000, 0));
        }

        private IEnumerator Start()
        {
            configurator.OnLoaded.AddListener(AfterLoading);
            yield return configurator.Execute();
            Coordinates.CoordinateSystems.SetOrigin(configurator.Configuration.Origin);
        }

        private void AfterLoading(Configuration configuration)
        {
            Debug.Log("Finished loading configuration");
            OnLoadedConfiguration.Invoke(configuration);
        }
    }
}