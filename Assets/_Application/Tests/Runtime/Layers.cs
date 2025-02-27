using NUnit.Framework;

namespace Netherlands3D.Twin.Tests
{
    public class Layers : TestCase
    {
        [Test]
        public void CanOpenLayerPanel()
        {
            Assert.IsFalse(Sidebar.Inspectors.Layers.IsOpen);
            
            Sidebar.ToolButtons.Layers.Click();
            E2E.Then(Sidebar.Inspectors.Layers.IsOpen, Is.True);
        }

        [Test]
        public void TerrainLayerShouldBeVisible()
        {
            if (Sidebar.Inspectors.Layers.IsOpen == false)
            {
                Sidebar.ToolButtons.Layers.Click();
            }

            var terrainLayer = Sidebar.Inspectors.Layers.Maaiveld;

            E2E.Then(terrainLayer.Visibility.IsOn, Is.True);
            E2E.Then(terrainLayer.IsActive, Is.True);
            
            // TODO: Move these calls into the Page Object Model
            E2E.Then(E2E.Find("Functionalities/CartesianTiles/Maaiveld(Clone)").Value.activeSelf, Is.False);
        }

        [Test]
        public void TerrainLayerCanBeMadeBeInvisible()
        {
            if (Sidebar.Inspectors.Layers.IsOpen == false)
            {
                Sidebar.ToolButtons.Layers.Click();
            }
            
            var terrainLayer = Sidebar.Inspectors.Layers.Maaiveld;

            terrainLayer.Visibility.Toggle();
            E2E.Then(terrainLayer.Visibility.IsOn, Is.False);
            E2E.Then(terrainLayer.IsActive, Is.False);

            // TODO: Move these calls into the Page Object Model
            E2E.Then(E2E.Find("Functionalities/CartesianTiles/Maaiveld(Clone)").Value.activeSelf, Is.False);
        }
    }
}