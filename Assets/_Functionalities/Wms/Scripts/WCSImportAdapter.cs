using System;
using System.IO;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Functionalities.NetCDF;
using Netherlands3D.Functionalities.NetCDF.LayerPresets;
using Netherlands3D.Functionalities.Wcs;
using Netherlands3D.Functionalities.Wms;
using Netherlands3D.Functionalities.Wms.LayerPresets;
using Netherlands3D.Legend;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.LayerPresets;
using UnityEngine;

namespace Netherlands3D.Functionalities.Wcs
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/WCSImportAdapter", fileName = "WCSImportAdapter", order = 0)]
    public class WCSImportAdapter : ScriptableObject, IDataTypeAdapter<LayerPresetArgs[]>
    {
        [SerializeField] private WCSLayerGameObject layerPrefab;

        public bool Supports(LocalFile localFile)
        {
            var cachedDataPath = localFile.LocalFilePath;
            var url = OgcWebServicesUtility.NormalizeUrl(localFile.SourceUrl);

            var bodyContents = File.ReadAllText(cachedDataPath);

            // if this is not a capabilities uri, it should be a GetMap uri; otherwise we do not support this
            if (!OgcWebServicesUtility.IsSupportedGetCapabilitiesUrl(url, bodyContents, ServiceType.Wcs))
            {
                return OgcWebServicesUtility.IsValidUrl(url, ServiceType.Wcs, RequestType.GetCoverage);
            }

            var request = new WcsGetCapabilities(url, bodyContents);

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
            var folderPreset = new FolderPreset.Args(url.AbsoluteUri);

            var cachedDataPath = localFile.LocalFilePath;
            var bodyContents = File.ReadAllText(cachedDataPath);

            if (OgcWebServicesUtility.IsSupportedGetCapabilitiesUrl(url, bodyContents, ServiceType.Wcs))
            {
                var request = new WcsGetCapabilities(url, bodyContents);
                BoundingBoxCache.AddBoundingBoxContainer(request);

                var coverageNames = request.GetLayerNames();

                var presets = new LayerPresetArgs[coverageNames.Count + 1];
                presets[0] = folderPreset;

                for (var i = 0; i < coverageNames.Count; i++)
                {
                    var map = new MapFilters
                    {
                        name = coverageNames[i],
                        version = request.GetVersion(),
                        width = layerPrefab.Resolution.x,
                        height = layerPrefab.Resolution.y,
                        spatialReferenceType = "CRS",
                        spatialReference = request.GetSpatialReference(coverageNames[i])
                    };

                    var preset = CreatePreset(map, url);
                    presets[i + 1] = preset;
                }

                return presets;
            }

            if (OgcWebServicesUtility.IsValidUrl(url, ServiceType.Wcs, RequestType.GetCoverage))
            {
                var request = new GetCoverageRequest(url, bodyContents);

                var map = request.CreateMapFromCapabilitiesUrl(
                    url,
                    layerPrefab.Resolution.x,
                    layerPrefab.Resolution.y
                );

                var preset = CreatePreset(map, url);
                return new[] { preset };
            }

            throw new ArgumentException("Unrecognized WCS request type: " + url);
        }

        private LayerPresetArgs CreatePreset(MapFilters mapFilters, Uri url)
        {
            return new WCSLayerPreset.Args(url, mapFilters);
        }
    }
}