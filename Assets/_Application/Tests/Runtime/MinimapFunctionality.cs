using System.Collections;
using Netherlands3D.Minimap;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Netherlands3D.Twin.Tests
{
    public class MinimapFunctionality
    {
        [UnitySetUp]
        public IEnumerator LoadSceneOnce()
        {
            yield return E2E.EnsureMainSceneIsLoaded();
        }

        [Test]
        public void MinimapIsShownOnScreen()
        {
            var minimap = E2E.Get.Component<MinimapUI>();
            minimap.Assert(Is.Not.Null, "No active object of type Minimap UI was found in the current scene");
        }

        [Test]
        public void MinimapHasAPinShowingTheCurrentPosition()
        {
            var wmtsMap = E2E.Get.Component<MinimapUI>().Component<WMTSMap>();
            
            wmtsMap.Assert(Is.InstanceOf<WMTSMap>(), "No instance of WMTSMap was found in the minimap");
            wmtsMap.GameObject("Pointer").Assert(Is.Not.Null, "No pointer could be found on the minimap");
        }
    }
}