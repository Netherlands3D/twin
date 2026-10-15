using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Utility;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "GeoJSON")]
    public class GeoJSONPropertyData : LayerPropertyData
    {
        [DataMember] public Coordinate geojsonCoordinate;
        
        //this needs to be saved in case the annotation is moved but connected to a geojson,
        //we need its start position as a key to find the corresponding geojson data
        public Coordinate GeoJSONCoordinate => geojsonCoordinate;
       
        [JsonConstructor]
        public GeoJSONPropertyData(Coordinate geojsonCoordinate)
        {
            this.geojsonCoordinate = geojsonCoordinate;
        }
    }
}