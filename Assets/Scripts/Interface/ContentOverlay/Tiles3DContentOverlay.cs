using System.Collections;
using System.Collections.Generic;
using Netherlands3D.TileSystem;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class Tiles3DContentOverlayInspector : OverlayInspector
    {
        [Tooltip("The same URL input is used here, as the one used in the property panel")]
        [SerializeField] private Tile3DLayerPropertySection tile3DLayerPropertySection;

        [Tooltip("The same credentials input is used here, as the one used in the property panel")]
        [SerializeField] private CredentialsPropertySection credentialsPropertySection;

        public override void SetReferencedLayer(ReferencedLayer layer)
        {
            base.SetReferencedLayer(layer);

            var tile3DLayer = layer as Tile3DLayer2;
            tile3DLayerPropertySection.Layer = tile3DLayer;
            credentialsPropertySection.LayerWithCredentials = tile3DLayer;
        }
    }
}
