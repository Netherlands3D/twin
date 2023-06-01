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
        [Tooltip("In the editor we do not get a URL; with this field you can simulate one")]
        private string debugUrl = "https://netherlands3d.eu/twin/?origin=155000,463000,200&features=terrain,buildings";

        public UnityEvent<Configuration> OnLoaded = new();

        public void Execute()
        {
            var url = Application.absoluteURL;
            #if UNITY_EDITOR
            url = debugUrl;
            #endif

            if (configuration.LoadFromUrl(url) == false)
            {
                StartSetup();
                return;
            }
            
            OnLoaded.Invoke(configuration);
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