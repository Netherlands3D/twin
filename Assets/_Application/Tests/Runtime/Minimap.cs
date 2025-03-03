using System.Collections;
using UnityEngine.TestTools;

namespace Netherlands3D.Twin.Tests
{
    public class Minimap : TestCase
    {
        [UnityTest]
        public IEnumerator MinimapIsShownOnScreen()
        {
            E2E.Then(
                WorldView.Minimap.IsActive,
                message: "No active object of type Minimap UI was found in the current scene"
            );
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator MinimapHasAPointer()
        {
            E2E.Then(WorldView.Minimap.Pointer.IsActive, message: "No pointer could be found on the minimap");

            yield return null;
        }
    }
}