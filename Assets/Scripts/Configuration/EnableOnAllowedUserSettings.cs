using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Configuration
{
    public class EnableOnAllowedUserSettings : MonoBehaviour
    {
        [Header("Disable this GameObject if user settings are disabled in the configuration file")]
        [SerializeField] private Configuration configuration;

        private void Awake() {
            //Configuration is not yet loaded in Awake, so we determine our active state using a persistent listener
            configuration.OnAllowUserSettingsChanged.AddListener(OnAllowUserSettingsChanged);
            gameObject.SetActive(false);
        }

        private void OnDestroy() {
            configuration.OnAllowUserSettingsChanged.RemoveListener(OnAllowUserSettingsChanged);
        }

        private void OnAllowUserSettingsChanged(bool userSettingsAllowed)
        {
            gameObject.SetActive(userSettingsAllowed);
        }
    }
}
