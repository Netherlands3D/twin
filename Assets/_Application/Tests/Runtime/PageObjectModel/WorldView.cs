using Netherlands3D.E2ETesting.PageObjectModel;
using Netherlands3D.Minimap;
using UnityEngine;

namespace Netherlands3D.Twin.Tests.PageObjectModel
{
    // See the README.md for information on Page Object Models
    public class WorldView : Element
    {
        public Element<GameObject> DefaultMaaiveld => E2E.Find("Functionalities/CartesianTiles/Maaiveld");

        public WorldviewElements.Minimap Minimap => WorldviewElements.Minimap.For(E2E.FindComponentOfType<MinimapUI>());
    }
}