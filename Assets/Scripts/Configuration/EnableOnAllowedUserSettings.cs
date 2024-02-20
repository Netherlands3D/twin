using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Configuration
{
    public class EnableOnAllowedUserSettings : MonoBehaviour
    {
        [Header("Disable this GameObject if user settings are disabled in the configuration file")]
        [SerializeField] private Configuration configuration;

        private void Awake()
        {
            //The active state will be determined by our configuration file. (Configuration load is later than Awake)
            configuration.OnAllowUserSettingsChanged.AddListener(OnAllowUserSettingsChanged);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            configuration.OnAllowUserSettingsChanged.RemoveListener(OnAllowUserSettingsChanged);
        }

        private void OnAllowUserSettingsChanged(bool userSettingsAllowed)
        {
            gameObject.SetActive(userSettingsAllowed);
        }
    }
}
