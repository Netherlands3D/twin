using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ReferenceLayer : LayerNL3DBase
    {
        public ReferencedLayer Reference { get; set; }
        
        public override bool IsActiveInScene
        {
            get => Reference.IsActiveInScene;
            set => Reference.IsActiveInScene = value;
        }
    }
}
