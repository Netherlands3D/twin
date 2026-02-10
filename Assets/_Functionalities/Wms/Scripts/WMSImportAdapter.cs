using System;
using System.IO;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Functionalities.Wms.LayerPresets;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Services;
using UnityEngine;

namespace Netherlands3D.Functionalities.Wms
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/WMSImportAdapter", fileName = "WMSImportAdapter", order = 0)]
    public class WMSImportAdapter : ScriptableObject, IDataTypeAdapter<LayerPresetArgs[]>
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

        public LayerPresetArgs[] Execute(LocalFile localFile)
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

                var presets = new LayerPresetArgs[maps.Count];
                for (var i = 0; i < maps.Count; i++) //todo test if this is now performant due to async visualisations
                {
                    var map = maps[i];
                    var preset = CreatePreset(map, url, i < layerPrefab.DefaultEnabledLayersMax);
                    presets[i] = preset;
                }

                // we return the parent layer, the sub layers will be created internally by the parent
                return presets;
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
                var preset = CreatePreset(map, url, true);
                return new[] { preset };
            }

            throw new ArgumentException("Unrecognized WMS request type: " + url);
        }

        private LayerData AddFolderLayer(string folderName)
        {
            var builder = new LayerBuilder().OfType("folder").NamedAs(folderName); //todo: make preset?
            var wfsFolder = App.Layers.Add(builder);
            return wfsFolder.LayerData;
        }

        private LayerPresetArgs CreatePreset(MapFilters mapFilters, Uri url, bool defaultEnabled)
        {
            return new WmsLayerPreset.Args(url, mapFilters, defaultEnabled);
        }
    }
}