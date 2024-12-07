using System.Collections;
using Netherlands3D.Minimap;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Netherlands3D.Functionalities.Minimap
{
    public class MinimapTest
    {
        private bool sceneLoaded = false;

        [UnitySetUp]
        public IEnumerator LoadSceneOnce()
        {
            // TODO: Move this to a custom attribute or helper method
            if (sceneLoaded) yield break;

            var asyncOperation = SceneManager.LoadSceneAsync("ConfigLoader");
            while (!asyncOperation.isDone) yield return null;
            while (SceneManager.GetActiveScene().name != "Main") yield return null;
            sceneLoaded = true;
        }

        [Test]
        public void MinimapIsShownOnScreen()
        {
            Debug.LogWarning("Running test");
            var minimap = Object.FindObjectOfType<MinimapUI>();
            
            Assert.That(minimap, Is.InstanceOf<MinimapUI>(), "No active object of type Minimap UI was found in the current scene");
        }
    }
}
