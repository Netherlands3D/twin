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
            configuration.OnAllowUserSettingsChanged.AddListener(OnAllowUserSettingsChanged);
            gameObject.SetActive(configuration.AllowUserSettings);
        }

        private void OnDestroy()
        {
            configuration.OnAllowUserSettingsChanged.RemoveListener(OnAllowUserSettingsChanged);
        }

        private void OnAllowUserSettingsChanged(bool userSettingsAllowed)
        {
            Debug.Log($"User settings allowed: {userSettingsAllowed}");
            gameObject.SetActive(userSettingsAllowed);
        }
    }
}
