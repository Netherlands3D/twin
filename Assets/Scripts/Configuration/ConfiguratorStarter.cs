using System.Collections;
using System.Globalization;
using System.Threading;
using UnityEngine;
using Netherlands3D.Coordinates;

namespace Netherlands3D.Twin.Configuration
{
    public class ConfiguratorStarter : MonoBehaviour
    {
        [SerializeField] private Configurator configurator;

        /// <summary>
        /// Make sure to set the culture to invariant to prevent issues with parsing floats and doubles.
        /// This way we consistently use the dot as the decimal separator.
        /// </summary>
        private void Awake() {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        }

        /// <summary>
        /// The configuration will affect all systems that first need to be initialized on Start. Because of this,
        /// the loading of the configuration needs to happen in the second frame (hence the yield return null) as to
        /// ensure everything has completed loading. Without this, you will get all kinds of weird Null exceptions
        /// and the progressbar won't show.
        /// </summary>
        /// 

        private void Awake()
        {
            CoordinateSystems.connectedCoordinateSystem = CoordinateSystem.RDNAP;
            CoordinateSystems.SetOrigin(new Coordinate(CoordinateSystem.RDNAP, 120000, 480000,0));
        }
        private IEnumerator Start()
        {
            yield return null;

            configurator.OnLoaded.AddListener(AfterLoading);
            yield return configurator.Execute();
        }

        public void ReopenSetup()
        {
            configurator.RestartSetup();
        }

        private void AfterLoading(Configuration configuration)
        {
            Debug.Log("Finished loading configuration");
        }
    }
}