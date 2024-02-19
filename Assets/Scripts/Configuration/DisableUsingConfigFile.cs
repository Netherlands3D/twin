using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Configuration
{
    public class DisableUsingConfigFile : MonoBehaviour
    {
        [Header("Disable this GameObject if the Configuration is loaded from a file")]
        [SerializeField] private Configuration configuration;

        private void Awake() {
            //Configuration is not yet loaded in Awake, so we determine our active state using a persistent listener
            configuration.OnShouldStartSetupChanged.AddListener(OnShouldStartSetupChanged);
            gameObject.SetActive(false);
        }

        private void OnDestroy() {
            configuration.OnShouldStartSetupChanged.RemoveListener(OnShouldStartSetupChanged);
        }

        private void OnShouldStartSetupChanged()
        {
            gameObject.SetActive(configuration.ShouldStartSetup);
        }
    }
}
