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
            var minimap = Object.FindObjectOfType<MinimapUI>();
            
            Assert.That(minimap, Is.InstanceOf<MinimapUI>(), "No active object of type Minimap UI was found in the current scene");
        }

        [Test]
        public void MinimapHasAPinShowingTheCurrentPosition()
        {
            var minimap = Object.FindObjectOfType<MinimapUI>();
            var wmtsMap = minimap.transform.GetComponentInChildren<WMTSMap>();
            var pointer = wmtsMap.transform.Find("Pointer");
            
            Assert.That(pointer, Is.Not.Null, "No pointer could be found on the minimap");
        }
    }
}
