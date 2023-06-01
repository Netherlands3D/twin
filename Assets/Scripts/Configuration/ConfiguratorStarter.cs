using UnityEngine;

namespace Netherlands3D.Twin.Configuration
{
    public class ConfiguratorStarter : MonoBehaviour
    {
        [SerializeField]
        private Configurator configurator;

        private void Awake()
        {
            configurator.Execute();
        }
    }
}