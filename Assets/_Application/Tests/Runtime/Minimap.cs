using NUnit.Framework;

namespace Netherlands3D.Twin.Tests
{
    public class Minimap : TestCase
    {
        [Test]
        public void MinimapIsShownOnScreen()
        {
            E2E.Then(
                WorldView.Minimap.IsActive, 
                Is.True, 
                "No active object of type Minimap UI was found in the current scene"
            );
        }

        [Test]
        public void MinimapHasAPointer()
        {
            E2E.Then(
                WorldView.Minimap.Pointer.IsActive, 
                Is.True, 
                "No pointer could be found on the minimap"
            );
        }
    }
}