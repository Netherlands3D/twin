using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Netherlands3D.Twin.Tests
{
    public class Minimap : TestCase
    {
        [UnityTest]
        [Category("Minimap")]
        public IEnumerator MinimapIsShownOnScreen()
        {
            E2E.Then(
                Scene.Minimap.IsActive,
                message: "No active object of type Minimap UI was found in the current scene"
            );
            
            yield return null;
        }

        [UnityTest]
        [Category("Minimap")]
        public IEnumerator MinimapHasAPointer()
        {
            E2E.Then(Scene.Minimap.Pointer.IsActive, message: "No pointer could be found on the minimap");

            yield return null;
        }
    }
}