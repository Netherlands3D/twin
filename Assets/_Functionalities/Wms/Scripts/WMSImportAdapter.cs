using System;
using System.IO;
using KindMen.Uxios;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Functionalities.Wms.LayerPresets;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Services;
using UnityEngine;

namespace Netherlands3D.Functionalities.Wms
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/WMSImportAdapter", fileName = "WMSImportAdapter", order = 0)]
    public class WMSImportAdapter : ScriptableObject, IDataTypeAdapter<Layer>
    {
        [SerializeField] private WMSLayerGameObject layerPrefab;

        public bool Supports(LocalFile localFile)
        {
            var cachedDataPath = localFile.LocalFilePath;
            var url = OgcWebServicesUtility.NormalizeUrl(localFile.SourceUrl);

            var bodyContents = File.ReadAllText(cachedDataPath);

            // if this is not a capabilities uri, it should be a GetMap uri; otherwise we do not support this
            if (!OgcWebServicesUtility.IsSupportedGetCapabilitiesUrl(url, bodyContents, ServiceType.Wms))
            {
                return OgcWebServicesUtility.IsValidUrl(url, ServiceType.Wms, RequestType.GetMap);
            }
            
            var request = new WmsGetCapabilities(url, bodyContents);
                
            // it should not just be a capabilities file, we also want to support BBOX!
            if (!request.CapableOfBoundingBoxes)
            {
                Debug.Log("<color=orange>WMS BBOX filter not supported.</color>");
                return false;
            }
            return true;
        }

        public Layer Execute(LocalFile localFile)
        {
            var url = OgcWebServicesUtility.NormalizeUrl(localFile.SourceUrl);
            var wmsFolder = AddFolderLayer(url.AbsoluteUri);

            var cachedDataPath = localFile.LocalFilePath;
            var bodyContents = File.ReadAllText(cachedDataPath);

            if (OgcWebServicesUtility.IsSupportedGetCapabilitiesUrl(url, bodyContents, ServiceType.Wms))
            {
                var request = new WmsGetCapabilities(url, bodyContents);
                BoundingBoxCache.AddBoundingBoxContainer(request);

                var maps = request.GetMaps(
                    layerPrefab.PreferredImageSize.x, 
                    layerPrefab.PreferredImageSize.y,
                    layerPrefab.TransparencyEnabled
                );
                
                for (var i = 0; i < maps.Count; i++) //todo test if this is now performant due to async visualisations
                {
                    var map = maps[i];
                    CreateLayer(map, url, wmsFolder, i < layerPrefab.DefaultEnabledLayersMax);
                }

                // we return the parent layer, the sub layers will be created internally by the parent
                return new Layer(wmsFolder);
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
                return CreateLayer(map, url, wmsFolder, true);
            }
            
            Debug.LogError("Unrecognized WMS request type at " + url);
            return null;
        }

        private LayerData AddFolderLayer(string folderName)
        {
            var builder = new LayerBuilder().OfType("folder").NamedAs(folderName); //todo: make preset?
            var wfsFolder = App.Layers.Add(builder);
            return wfsFolder.LayerData;
        }

        private Layer CreateLayer(MapFilters mapFilters, Uri url, LayerData folderLayer, bool defaultEnabled)
        {
            return App.Layers.Add(
                new WmsLayerPreset.Args(url, mapFilters, folderLayer, defaultEnabled)
            );
        }
    }
}
