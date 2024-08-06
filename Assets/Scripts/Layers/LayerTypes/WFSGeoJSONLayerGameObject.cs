using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using GeoJSON.Net;
using GeoJSON.Net.CoordinateReferenceSystem;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using SimpleJSON;
using UnityEngine.Events;
using Netherlands3D.Twin.Layers.Properties;
using System.Linq;
using Netherlands3D.CartesianTiles;

namespace Netherlands3D.Twin.Layers
{
    /// <summary>
    /// Extention of GeoJSONLayerGameObject that injects a 'streaming' dataprovider
    /// </summary>
    public class WFSGeoJsonLayerGameObject : GeoJsonLayerGameObject, ILayerWithPropertyData
    {
        [SerializeField] private WFSGeoJSONTileDataLayer cartesianTileWFSLayer;

        public WFSGeoJSONTileDataLayer CartesianTileWFSLayer { get => cartesianTileWFSLayer; }

        private void Awake() {
            CartesianTileWFSLayer.WFSGeoJSONLayer = this;
            CartesianTileWFSLayer.WfsUrl = "";
        }
    }
}