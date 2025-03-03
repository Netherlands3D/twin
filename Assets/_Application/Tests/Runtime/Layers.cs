using System;
using System.Collections;
using Netherlands3D.CartesianTiles;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Netherlands3D.Twin.Tests
{
    public class Layers : TestCase
    {
        // Warning: Ordering tests is not recommended and should only be done for performance reasons and if
        // there are unexpected side-effects when tests run out of order.
        //
        // Tests should also be written in such a way they do not depend on the previous state, this enables them to be
        // written independently
        private struct TestMoment
        {
            public const int Any = 0;
            public const int WhileOpeningLayerPanel = 0;
            public const int WhenLayerPanelIsOpen = 1;
            public const int WhileTerrainInvisible = 2;
        }

        [UnityTest]
        [Order(TestMoment.WhileOpeningLayerPanel)]
        [Category("Layers")]
        public IEnumerator CanOpenLayerPanel()
        {
            yield return E2E.Assume(() => Sidebar.Inspectors.Layers.IsOpen, Is.False);
            
            Sidebar.ToolButtons.Layers.Click();

            E2E.Then(Sidebar.Inspectors.Layers.IsOpen);
        }

        [UnityTest]
        [Order(TestMoment.WhenLayerPanelIsOpen)]
        [Category("Layers")]
        [Category("Layers/CartesianTiles")]
        public IEnumerator TerrainLayerShouldBeVisible()
        {
            Sidebar.LayerPanelShouldBeOpen();

            var terrainLayer = Sidebar.Inspectors.Layers.Maaiveld;

            E2E.Then(terrainLayer.Visibility.IsOn);
            E2E.Then(terrainLayer.IsActive);
            yield return E2E.Expect(() => Scene.DefaultMaaiveld.IsActive);
        }

        [UnityTest]
        [Order(TestMoment.WhileTerrainInvisible)]
        [Category("Layers")]
        [Category("Layers/CartesianTiles")]
        public IEnumerator TerrainLayerCanBeMadeBeInvisible()
        {
            Sidebar.LayerPanelShouldBeOpen();
            
            yield return E2E.Assume(
                IsMaaiveldEnabled,
                message: "Maaiveld is expected to be visible at the start of this test"
            );

            var terrainLayer = Sidebar.Inspectors.Layers.Maaiveld;
            
            terrainLayer.Visibility.Toggle();
            
            E2E.ThenNot(terrainLayer.Visibility.IsOn);
            E2E.ThenNot(terrainLayer.IsActive);

            yield return E2E.Expect(
                IsMaaiveldEnabled, 
                Is.False,
                message: "Maaiveld is expected to be invisible, but wasn't"
            );
        }

        private object IsMaaiveldEnabled()
        {
            return ((BinaryMeshLayer)Scene.DefaultMaaiveld.Component<BinaryMeshLayer>())?.isEnabled;
        }
    }
}