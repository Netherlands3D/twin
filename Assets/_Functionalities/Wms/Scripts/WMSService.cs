using UnityEngine;
using Netherlands3D.Twin.Services;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using System;
using Netherlands3D.WMSTiles3D;
using System.Collections.Generic;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Functionalities.Wms;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Utility;
using Netherlands3D.Credentials;
using GG.Extensions;
using System.Net.Security;
using System.Linq;

namespace Netherlands3D.Functionalities.WMS
{


    public class WMSService : MonoBehaviour
    {
         struct layerproperties
           { 
                public string name;
                public string style;
                public int rootid;
            
            }
        private struct wmsLayerSettings
        {
            public string layername;
            public string layerStyle;
            public WMSTileFunctions wmsServer;
        }

        private List<WMSTileFunctions>wmsServers = new List<WMSTileFunctions>();
        private Dictionary<LayerData,wmsLayerSettings> layerDictionary = new Dictionary<LayerData, wmsLayerSettings>();
        private Layers layers;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        public GameObject projectorPrefab;
        
        public TilePrioritiser tileprioritiser;

        void Start()
        {
        layers = App.Layers;
        layers.LayerAdded.AddListener(OnLayerAdded);
        layers.LayerRemoved.AddListener(OnLayerRemoved);
        

        }

        // Update is called once per frame
        void Update()
        {
        
        }

        void OnLayerAdded(LayerData layerData)
        {
            layerData.LayerActiveInHierarchyChanged.AddListener(OnLayerActiveInHierarchyChanged);
            layerData.LayerOrderChanged.AddListener(OnLayerOrderChanged);
            
            if(IsWMSLayerData(layerData)==false)return;
            if(layerDictionary.ContainsKey(layerData)==false)
            {
                wmsLayerSettings layerSettings = new wmsLayerSettings();
                LayerURLPropertyData urlPropertydata = layerData.GetProperty<LayerURLPropertyData>();
                layerSettings.layername = OgcWebServicesUtility.GetParameterFromURL(urlPropertydata.Url.OriginalString,"layers");
                layerSettings.layerStyle = OgcWebServicesUtility.GetParameterFromURL(urlPropertydata.Url.OriginalString,"styles");
                string baseUrl = urlPropertydata.Url.AbsoluteUri.Split("?")[0];
                WMSTileFunctions wmsServer = null;
                for (int i=0;i<wmsServers.Count;i++)
                {
                    string serverurl = wmsServers[i].tilesetUrl.Split("?")[0];
                    if (string.Equals(serverurl,baseUrl))
                    {
                        wmsServer = wmsServers[i];
                    }
                }
                if(wmsServer == null)
                {
                    //create new wmsServer
                    wmsServer = createWMSServer(layerData);
                    wmsServers.Add(wmsServer);
                }
                if (wmsServer!=null)
                {// add the layer to the wmsServer
                layerSettings.wmsServer = wmsServer;
                layerDictionary.Add(layerData,layerSettings);
                ChangeActiveLayers(wmsServer);
                }
            }
        }
        
        void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            foreach(var wmsserver in wmsServers)
            {
                ChangeActiveLayers(wmsserver);
            }
        }

        void OnLayerOrderChanged(int index)
        {
            
        }
        void OnLayerRemoved(LayerData layerData, bool boolean)
        {
            if(layerDictionary.ContainsKey(layerData))
            {
                wmsLayerSettings server = layerDictionary[layerData];
                layerData.LayerActiveInHierarchyChanged.RemoveListener(OnLayerActiveInHierarchyChanged);
                layerData.LayerOrderChanged.RemoveListener(OnLayerOrderChanged);
                
                layerDictionary.Remove(layerData);
                ChangeActiveLayers(server.wmsServer);
            }
        }
        bool IsWMSLayerData(LayerData layerData)
        {
            if(layerData is RootLayer) return false;
            int layerHashCode = layerData.ParentLayer.GetHashCode();
             if(layerData.HasProperties)
            {
                LayerURLPropertyData urlPropertydata = layerData.GetProperty<LayerURLPropertyData>();
                if(urlPropertydata!=null)
                {
                    Uri url = urlPropertydata.Url;
                    
                    if(url.OriginalString.Contains("wms", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    
        WMSTileFunctions createWMSServer(LayerData layerData)
        {
             var urlPropertyData = layerData.GetProperty<LayerURLPropertyData>();
            if (urlPropertyData.Url == null) return null;

            wmsServerProperties wmsServerproperties = getWMSServerProperties(layerData);    

            GameObject wmsServerGameobject = new GameObject();
            wmsServerGameobject.transform.parent = transform;
            WMSTileFunctions wmsTilefunctions = wmsServerGameobject.AddComponent<WMSTileFunctions>();
            
            wmsTilefunctions.tilesetUrl = urlPropertyData.Url.AbsoluteUri;
            wmsTilefunctions.projectorPrefab = projectorPrefab;
            wmsTilefunctions.tilePrioritiser = tileprioritiser;
            wmsTilefunctions.wmsServerProperties = wmsServerproperties;
            wmsTilefunctions.mapfilter = MapFilters.FromUrl(urlPropertyData.Url);
            wmsTilefunctions.Initialize();          
            return wmsTilefunctions;
        }

        wmsServerProperties getWMSServerProperties(LayerData layerData)
        {
            var urlPropertyData = layerData.GetProperty<LayerURLPropertyData>();
                       
            MapFilters mapfilter = MapFilters.FromUrl(urlPropertyData.Url);
            wmsServerProperties wmsLayerProperties = new wmsServerProperties();
            Coordinate BottomLeft = new Coordinate(CoordinateSystem.RD,-2000.0,290000.0);
            Coordinate Topright = new Coordinate(CoordinateSystem.RD, 294000.0,630000.0);
            wmsLayerProperties.boundingBox = new BoundingBox(BottomLeft,Topright);
            CoordinateSystems.FindCoordinateSystem(mapfilter.spatialReference);
            wmsLayerProperties.coordinateSystem = CoordinateSystems.FindCoordinateSystem(mapfilter.spatialReference);
            wmsLayerProperties.maxScaleDenominator = 150000;
            wmsLayerProperties.MinimumScreenSpaceError=2f;
            wmsLayerProperties.MaxTreeDepth=3;
            return wmsLayerProperties;

        }
    
        void ChangeActiveLayers(WMSTileFunctions wmsServer)
        {
           
            List<layerproperties>layerprops = new List<layerproperties>();

            foreach(KeyValuePair<LayerData,wmsLayerSettings> kvp in layerDictionary)
            {
                if(kvp.Value.wmsServer == wmsServer)
                {
                    if(kvp.Key.ActiveInHierarchy)
                    {
                        layerproperties layerprop = new layerproperties();
                        layerprop.name = kvp.Value.layername;
                        layerprop.style=kvp.Value.layerStyle;
                        layerprop.rootid=kvp.Key.RootId;
                        layerprops.Add(layerprop);
                    }
                }

            }
            List<layerproperties> orderedlayerprops = layerprops.OrderBy(o=>o.rootid).ToList();
            layerprops.Reverse();
            List<string> layernamestrings = new List<string>();
            List<string> layerstylestrings = new List<string>();
            for(int i=0;i<orderedlayerprops.Count;i++)
            {
                layernamestrings.Add(orderedlayerprops[i].name);
                layerstylestrings.Add(orderedlayerprops[i].style);

            }

            string layernamestring = string.Join(",",layernamestrings);
            string stylenamestring = string.Join(",",layerstylestrings);
            if(string.Equals(wmsServer.mapfilter.name,layernamestring)==false)
            {
                wmsServer.mapfilter.name = layernamestring;
                wmsServer.mapfilter.style = stylenamestring; 
                wmsServer.ReloadTileContent();
            }
        }
    }
}

