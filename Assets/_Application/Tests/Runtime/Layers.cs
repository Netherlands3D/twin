using Netherlands3D.Twin.Layers.UI.HierarchyInspector;
using NUnit.Framework;

namespace Netherlands3D.Twin.Tests
{
    public class Layers : E2ETestCase
    {
        [Test]
        public void CanOpenLayerPanel()
        {
            E2E.Given.LayerPanelIsOpen();

            var layerUIManager = E2E.FindComponentOfType<LayerUIManager>();
            E2E.Then(layerUIManager, Is.Not.Null);
            E2E.Then(layerUIManager.Value.isActiveAndEnabled, Is.True);
        }
    }
}