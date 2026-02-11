using Netherlands3D.E2ETesting;
using UnityEngine;
using Netherlands3D.E2ETesting.PageObjectModel;

namespace Netherlands3D.Twin.Tests.Projectsettings
{
 public class Scene : Element
    {
        public Element<GameObject> DefaultMaaiveld => E2E.Find("Functionalities/CartesianTiles/Maaiveld (Clone)");
        public Element<GameObject> DefaultBuildings => E2E.Find("Functionalities/CartesianTiles/Gebouwen (Clone)");
        public Element<GameObject> DefaultBomen => E2E.Find("Functionalities/CartesianTiles/Bomen (Clone)");
        public Element<GameObject> DefaultBossen => E2E.Find("Functionalities/CartesianTiles/Bossen (Clone)");
        public Element<GameObject> LagenMenuButton => E2E.Find("CanvasUI/Base/Body/Sidebar & Toolbar/Toolbar/TopButtons/ToolbarButton_Layers");
        public Element<GameObject> Layerspanel => E2E.Find("CanvasUI/Base/Body/Sidebar & Toolbar/Sidebar/Content/LayersInspector(Clone)/LayersPanel/Layers");

    }
}