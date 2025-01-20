using UnityEngine;
using UnityEngine.SceneManagement;

namespace Netherlands3D.Twin.Configuration
{
    public class SceneSwitcher : MonoBehaviour
    {
        public void LoadSceneByName(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);            
        }
    }
}
