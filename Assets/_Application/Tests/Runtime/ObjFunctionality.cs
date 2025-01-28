using System.Collections;
using Netherlands3D.Minimap;
using Netherlands3D.Twin.Layers.UI.HierarchyInspector;
using Netherlands3D.Twin.Tools.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Tests
{
    public class ObjFunctionality
    {
        [UnitySetUp]
        public IEnumerator LoadSceneOnce()
        {
            yield return E2E.EnsureMainSceneIsLoaded();
        }

        [Test]
        public void ImportNewObjFile()
        {
            E2E.Sidebar.OpenLayers();

            var layerUIManager = E2E.Get.Component<LayerUIManager>();
            layerUIManager.Assert(Is.Not.Null);
            Assert.That(layerUIManager.Value.isActiveAndEnabled, Is.True);

            // GameObject.FindObjectOfType<Inspector>();
            // var minimap = Object.FindObjectOfType<MinimapUI>();

            // Assert.That(
                // minimap, 
                // Is.InstanceOf<MinimapUI>(),
                // "No active object of type Minimap UI was found in the current scene"
            // );
        }
    }
}