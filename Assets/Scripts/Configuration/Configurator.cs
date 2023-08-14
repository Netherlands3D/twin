using System;
using System.Collections;
using System.IO;
using SimpleJSON;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Netherlands3D.Twin.Configuration
{
    [CreateAssetMenu(menuName = "Netherlands3D/Twin/Configurator", fileName = "Configurator", order = 0)]
    public class Configurator : ScriptableObject
    {
        [SerializeField] 
        private Configuration configuration;

        [SerializeField] 
        [Tooltip("The scene with the Setup Wizard that needs to load additively")]
        private string setupSceneName;

        [SerializeField]
        [Tooltip("In the editor we do not get a URL; with this field you can simulate one. For example: https://netherlands3d.eu/twin/?origin=155000,463000,200&features=terrain,buildings")]
        private string debugUrl = "";

        [SerializeField]
        [Tooltip("The location where to get the configuration file from")]
        private string configFilePath = "app.config.json";

        public UnityEvent OnStartedLoading = new();
        public UnityEvent<Configuration> OnLoaded = new();

        public IEnumerator Execute()
        {
            OnStartedLoading.Invoke();
            var uri = Application.dataPath + '/' + configFilePath;
            yield return configuration.PopulateFromFile(uri);

            Debug.Log($"Loading configuration from URL");
            var url = Application.absoluteURL;
            #if UNITY_EDITOR
            url = debugUrl;
            #endif

            if (string.IsNullOrEmpty(url) || configuration.Populate(new Uri(url)) == false)
            {
                StartSetup();
            }

            OnLoaded.Invoke(configuration);
            yield return null;
        }
        
        [ContextMenu("Read config from json file")]
        public void ReadConfigFromJson()
        {
            var filePath = Application.dataPath + "/app-config.json";
            var jsonText = File.ReadAllText(filePath);
            configuration.Populate(JSON.Parse(jsonText));
            Debug.Log($"{filePath} read");
        }

        [ContextMenu("Write config to json file")]
        private void WriteConfigToJson()
        {
            var filePath = EditorUtility.SaveFilePanel(
                "Configuration file", 
                null, 
                "app-config", 
                "json"
            );
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogWarning("Cancelled saving of config file");
                return;
            }

            var jsonText = configuration.ToJsonNode().ToString();
            File.WriteAllText(filePath, jsonText);
            Debug.Log($"{filePath} created");
        }

        public void StartSetup()
        {
            SceneManager.LoadScene(setupSceneName, LoadSceneMode.Additive);
        }

        public void CompleteSetup()
        {
            // We assume the Setup Wizard modifies the configuration object; this is merely a hook for the rest of
            // the application to know that we are done.
            OnLoaded.Invoke(configuration);
            SceneManager.UnloadSceneAsync(setupSceneName);
        }
    }
}