using System;
using System.IO;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Functionalities.OgcWebServices.Shared;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Functionalities.Wms
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/WMSImportAdapter", fileName = "WMSImportAdapter", order = 0)]
    public class WMSImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private WMSLayerGameObject layerPrefab;

        public bool Supports(LocalFile localFile)
        {
            var cachedDataPath = localFile.LocalFilePath;
            var sourceUrl = localFile.SourceUrl;
            var url = new Uri(sourceUrl);

            var bodyContents = File.ReadAllText(cachedDataPath);

            // if this is not a capabilities uri, it should be a GetMap uri; otherwise we do not support this
            if (!OgcWebServicesUtility.IsSupportedGetCapabilitiesUrl(url, bodyContents, ServiceType.Wms))
            {
                return OgcWebServicesUtility.IsValidUrl(url, ServiceType.Wms, RequestType.GetMap);
            }
            
            var request = new WmsGetCapabilitiesRequest(url, bodyContents);
                
            // it should not just be a capabilities file, we also want to support BBOX!
            if (!request.CapableOfBoundingBoxes)
            {
                Debug.Log("<color=orange>WMS BBOX filter not supported.</color>");
                return false;
            }
            return true;
        }

        public void Execute(LocalFile localFile)
        {
            var url = new Uri(localFile.SourceUrl);
            var wmsFolder = new FolderLayer(url.AbsoluteUri);

            var cachedDataPath = localFile.LocalFilePath;
            var bodyContents = File.ReadAllText(cachedDataPath);

            if (OgcWebServicesUtility.IsSupportedGetCapabilitiesUrl(url, bodyContents, ServiceType.Wms))
            {
                var request = new WmsGetCapabilitiesRequest(url, bodyContents);
                WMSBoundingBoxCache.AddWmsBoundingBoxContainer(request);

                var maps = request.GetMaps(
                    layerPrefab.PreferredImageSize.x, 
                    layerPrefab.PreferredImageSize.y,
                    layerPrefab.TransparencyEnabled
                );
                for (int i = 0; i < maps.Count; i++)
                {
                    CreateLayer(maps[i], url, wmsFolder, i < layerPrefab.DefaultEnabledLayersMax);
                }

                return;
            }

            if (OgcWebServicesUtility.IsValidUrl(url, ServiceType.Wms, RequestType.GetMap))
            {
                var request = new GetMapRequest(url, bodyContents);
                var map = request.CreateMapFromCapabilitiesUrl(
                    url,
                    layerPrefab.PreferredImageSize.x, 
                    layerPrefab.PreferredImageSize.y,
                    layerPrefab.TransparencyEnabled
                );
                CreateLayer(map, url, wmsFolder);

                return;
            }
            
            Debug.LogError("Unrecognized WMS request type at " + url);
        }

        private void CreateLayer(MapFilters mapFilters, Uri url, FolderLayer folderLayer, bool defaultEnabled = true)
        {
            WMSLayerGameObject newLayer = Instantiate(layerPrefab);
            newLayer.LayerData.SetParent(folderLayer);
            newLayer.Name = mapFilters.name;
            newLayer.LayerData.ActiveSelf = defaultEnabled;
            
            url = mapFilters.ToUrlBasedOn(url);

            var propertyData = newLayer.PropertyData as LayerURLPropertyData;
            propertyData.Data = url;
        }
    }
}
