using System;
using System.Collections;
using System.IO;
using Netherlands3D.Twin.Features;
using Netherlands3D.Twin.Interface;
using SimpleJSON;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Netherlands3D.Twin.Configuration
{
    [CreateAssetMenu(menuName = "Netherlands3D/Twin/Configurator", fileName = "Configurator", order = 0)]
    public class Configurator : ScriptableObject, IWindow
    {
        [SerializeField] 
        private Configuration configuration;
        public Configuration Configuration { get => configuration; }

        [SerializeField] 
        [Tooltip("The scene with the Setup Wizard that needs to load additively")]
        private string setupSceneName;

        [SerializeField]
        [Tooltip("The location where to get the configuration file from")]
        private string configFilePath = "app.config.json";

        [Header("Debugging Aids")]
        [SerializeField]
        [Tooltip("In the editor we do not get a URL; with this field you can simulate one. For example: https://netherlands3d.eu/twin/?origin=155000,463000,200&features=terrain,buildings")]
        private string debugUrl = "";

        [SerializeField]
        [TextArea(20, 100)]
        [Tooltip("In the editor we do easily read a config file; with this field you can simulate one.")]
        private string debugConfig = "";

        [Header("Events")]
        public UnityEvent OnStartedLoading = new();
        public UnityEvent<Configuration> OnLoaded = new();
        public UnityEvent OnOpenInterface = new();
        public UnityEvent OnCloseInterface = new();
        public UnityEvent OnOpen { get => OnOpenInterface; }
        public UnityEvent OnClose { get => OnCloseInterface; }

        public bool SetupSceneLoaded { 
            get{
                return SceneManager.GetSceneByName(setupSceneName) == null || SceneManager.GetSceneByName(setupSceneName).isLoaded;
            } 
        }

        public bool IsOpen { 
            get => SetupSceneLoaded; 
            set
            {
                if (value)
                {
                    Open();
                }
                else
                {
                    Close();
                }
            }
        }

        public IEnumerator Execute()
        {
            configuration.ShouldStartSetup = true;
            OnStartedLoading.Invoke();
            
            #if UNITY_EDITOR
            if (string.IsNullOrEmpty(debugConfig) == false)
            {
                configuration.Populate(JSON.Parse(debugConfig));
                configuration.ShouldStartSetup = false;
            } else {
            #endif
            var uri = Application.dataPath + '/' + configFilePath;
            yield return configuration.PopulateFromFile(uri);
            #if UNITY_EDITOR
            }
            #endif

            var url = Application.absoluteURL;
            #if UNITY_EDITOR
            url = debugUrl;
            #endif

            if (string.IsNullOrEmpty(url) == false)
            {
                Debug.Log($"Loading configuration from URL '{url}'");
                configuration.Populate(new Uri(url));
            }

            if (configuration.ShouldStartSetup)
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
            #if UNITY_EDITOR
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
            #endif
        }

        public void StartSetup()
        {
            Open();
        }
        
        public void RestartSetup()
        {
            if (!configuration.ShouldStartSetup) return;

            StartSetup();
        }
        
        public void Open()
        {
            if (SetupSceneLoaded)
            {
                return;
            }

            SceneManager.LoadScene(setupSceneName, LoadSceneMode.Additive);
            OnOpenInterface.Invoke();
        }

        public void Close()
        {
            if (!SetupSceneLoaded)
            {
                return;
            }
            
            SceneManager.UnloadSceneAsync(setupSceneName);
            OnCloseInterface.Invoke();
        }

        public void CompleteSetup()
        {
            // We assume the Setup Wizard modifies the configuration object; this is merely a hook for the rest of
            // the application to know that we are done.
            Close();
            OnLoaded.Invoke(configuration);
            SceneManager.UnloadSceneAsync(setupSceneName);
        }
    }
}