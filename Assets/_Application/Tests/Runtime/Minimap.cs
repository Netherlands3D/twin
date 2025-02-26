using Netherlands3D.Minimap;
using NUnit.Framework;

namespace Netherlands3D.Twin.Tests
{
    public class Minimap : E2ETestCase
    {
        [Test]
        public void MinimapIsShownOnScreen()
        {
            var minimap = E2E.FindComponentOfType<MinimapUI>();
            
            E2E.Then(minimap.Value, Is.Not.Null, "No active object of type Minimap UI was found in the current scene");
        }

        [Test]
        public void MinimapHasAPinShowingTheCurrentPosition()
        {
            var wmtsMap = E2E.FindComponentOfType<MinimapUI>().Component<WMTSMap>();
            
            E2E.Then(wmtsMap, Is.InstanceOf<WMTSMap>(), "No instance of WMTSMap was found in the minimap");
            E2E.Then(wmtsMap.GameObject("Pointer"), Is.Not.Null, "No pointer could be found on the minimap");
        }
    }
}