using Netherlands3D.E2ETesting.PageObjectModel;
using Netherlands3D.Minimap;
using UnityEngine;

namespace Netherlands3D.Twin.Tests.PageObjectModel.WorldviewElements
{
    public class Minimap : Element<MinimapUI, Minimap>
    {
        public Element<GameObject> Pointer { get; private set; }

        protected override void Setup()
        {
            Pointer = Component<WMTSMap>()?.GameObject("Pointer");
        }
    }
}