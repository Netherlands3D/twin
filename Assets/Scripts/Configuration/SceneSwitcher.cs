using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Netherlands3D.Twin
{
    public class SceneSwitcher : MonoBehaviour
    {
        public void LoadSceneByName(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);            
        }
    }
}
