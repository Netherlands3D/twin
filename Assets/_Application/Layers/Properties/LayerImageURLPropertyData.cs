using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "ImageUrl")]
    public class LayerImageURLPropertyData : LayerURLPropertyData //Hack: we need to be able to distinguish between 2 urls added to the same layer.
    {
       
    }
}
