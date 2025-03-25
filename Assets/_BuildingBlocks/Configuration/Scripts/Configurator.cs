using System;
using System.Collections;
using System.IO;
using KindMen.Uxios;
using KindMen.Uxios.Interceptors;
using KindMen.Uxios.Interceptors.NetworkInspector;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.Twin.Interface;
using Netherlands3D.Twin.Projects;
using SimpleJSON;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using RequestInterceptor = KindMen.Uxios.Interceptors.RequestInterceptor;

namespace Netherlands3D.Twin.Configuration
{
    [CreateAssetMenu(menuName = "Netherlands3D/Twin/Configurator", fileName = "Configurator", order = 0)]
    public class Configurator : ScriptableObject, IWindow
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void ReplaceUrl(string url);
#endif

        [SerializeField] 
        private Configuration configuration;
        public Configuration Configuration { get => configuration; }

        private Uri uri;

        [SerializeField] 
        [Tooltip("The scene with the Setup Window that needs to load additively")]
        private string setupSceneName;

        [SerializeField]
        [Tooltip("The main scene that will be loaded after the setup scene is closed")]
        private string mainSceneName;

        [SerializeField]
        [Tooltip("The location where to get the configuration file from")]
        private string configFilePath = "app.config.json";

        [Header("Debugging Aids")]
        [SerializeField]
        [Tooltip("In the editor we do not get a URL; with this field you can simulate one. For example: https://netherlands3d.eu/twin/?origin=155000,463000,200&functionalities=terrain,buildings")]
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
        
#if UNITY_EDITOR
        [MenuItem("Netherlands3D/Change Debug Configuration")]
        public static void OpenConfiguratorInInspector()
        {
            var assetPath = AssetDatabase.GUIDToAssetPath("049e538b7dc9ec64789200c6804d8dbf");
            var myScriptableObject = AssetDatabase.LoadAssetAtPath(assetPath, typeof(ScriptableObject));
            Selection.activeObject = myScriptableObject;
        }
#endif

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
            configuration.ShouldStartSetup = false;
            var indicatorsConfiguration = GetFunctionalityConfigurationOfType<Netherlands3D.Functionalities.Indicators.Configuration.Configuration>();
            indicatorsConfiguration.OnDossierIdChanged.RemoveListener(UpdateDossierIdAfterLoading);
            
            OnStartedLoading.Invoke();
            
            #if UNITY_EDITOR
            if (string.IsNullOrEmpty(debugConfig) == false)
            {
                configuration.Populate(JSON.Parse(debugConfig));
                configuration.ShouldStartSetup = false;
            } else {
            #endif
                yield return configuration.PopulateFromFile(Application.dataPath + '/' + configFilePath);
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
                uri = new Uri(url);
                configuration.Populate(uri);
            }

            //this overwrites the url parameters with the project functionalities after loading a project
            configuration.AddProjectDataChangedListener();
            
            //add any missing functionalities to the projectData (e.g. when a project was saved before the functionality was added to the application)
            configuration.AddFunctionalityDataToProject();

            ConfigureCorsProxy();

            OnLoaded.Invoke(configuration);
            
            indicatorsConfiguration.OnDossierIdChanged.AddListener(UpdateDossierIdAfterLoading);

            SceneManager.sceneLoaded += (scene, mode) => {
                if(scene.name == mainSceneName && Configuration.ShouldStartSetup)
                {
                    StartSetup();
                }
            };

            yield return null;
        }

        private void ConfigureCorsProxy()
        {
            if (string.IsNullOrEmpty(configuration.CorsProxyUrl)) return;
            Debug.LogWarning("Configured CORS proxy is: " + configuration.CorsProxyUrl);

            // Add an Uxios interceptor to prefix all URLs with the given CORS Proxy URL - we assume that the chosen
            // proxy works by treating the real URL as a path to the proxy URL. This is consistent with how the 
            // CORS Anywhere proxy works, but if we were to use another proxy it may not work anymore.
            Uxios.DefaultInstance.Interceptors.request.Add(new RequestInterceptor(request =>
            {
                request.Url = new Uri($"{configuration.CorsProxyUrl}/{request.Url}");
                request.Headers.Add("X-Requested-With", "Netherlands3D");
                return request;
            }));
        }

        private T GetFunctionalityConfigurationOfType<T>() where T : ScriptableObject,IConfiguration
        {
            return configuration
                .Functionalities
                .Find(functionality => functionality.configuration is T)
                .configuration as T;
        }

        private void UpdateDossierIdAfterLoading(string dossierId)
        {
            var uriBuilder = new UriBuilder(uri); 
            GetFunctionalityConfigurationOfType<Netherlands3D.Functionalities.Indicators.Configuration.Configuration>().SetDossierIdInQueryParameters(uriBuilder);

            uri = uriBuilder.Uri;
#if UNITY_WEBGL && !UNITY_EDITOR
            ReplaceUrl($"./{uri.Query}");
#endif
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

        [ContextMenu("Write config to Debug Config field")]
        private void WriteConfigToDebugConfig()
        {
            debugConfig = configuration.ToJsonNode().ToString(4);
        }

        [ContextMenu("Write config to Debug Url field")]
        private void WriteConfigToDebugUrl()
        {
            debugUrl = $"https://netherlands3d.eu/twin/{configuration.ToQueryString()}";
        }

        public void StartSetup()
        {
            if (!configuration.ShouldStartSetup) return;

            Open();
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
            // We assume the Setup Window modifies the configuration object; this is merely a hook for the rest of
            // the application to know that we are done.
            Close();
            OnLoaded.Invoke(configuration);
        }
    }
}