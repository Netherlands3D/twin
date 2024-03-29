using System.Collections;
using UnityEngine;

namespace Netherlands3D.Twin.Configuration
{
    public class ConfiguratorStarter : MonoBehaviour
    {
        [SerializeField] private Configurator configurator;

        /// <summary>
        /// The configuration will affect all systems that first need to be initialized on Start. Because of this,
        /// the loading of the configuration needs to happen in the second frame (hence the yield return null) as to
        /// ensure everything has completed loading. Without this, you will get all kinds of weird Null exceptions
        /// and the progressbar won't show.
        /// </summary>
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