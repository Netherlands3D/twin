using System.Collections;
using UnityEngine;

namespace Netherlands3D.Twin.Configuration
{
    public class ConfiguratorStarter : MonoBehaviour
    {
        [SerializeField]
        private Configurator configurator;

        private IEnumerator Start()
        {
            // Wait one frame to give all other start methods a chance to initialize
            yield return null;

            configurator.Execute();
        }
    }
}