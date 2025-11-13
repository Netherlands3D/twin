using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Functionalities.OBJImporter.LayerPresets;
using Netherlands3D.Functionalities.Wms.LayerPresets;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Functionalities.Wms
{
    public class CoroutineRunner : MonoBehaviour
    {
    }

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
            
            var request = new WmsGetCapabilities(url, bodyContents);
                
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
                
                var coroutineRunner = new GameObject("WMSImportAdapter coroutine runner").AddComponent<CoroutineRunner>();
                coroutineRunner.StartCoroutine(CreateMapLayers(maps, url, wmsFolder, coroutineRunner.gameObject));

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
                CreateLayer(map, url, wmsFolder, true);

                return;
            }
            
            Debug.LogError("Unrecognized WMS request type at " + url);
        }

        private LayerData AddFolderLayer(string folderName)
        {
            var builder = new LayerBuilder().OfType("folder").NamedAs(folderName); //todo: make preset?
            var wfsFolder = App.Layers.Add(builder);
            return wfsFolder.LayerData;
        }
        
        private IEnumerator CreateMapLayers(List<MapFilters> maps, Uri url, LayerData wmsFolder, GameObject coroutineRunner)
        {
            for (int i = 0; i < maps.Count; i++)
            {
                CreateLayer(maps[i], url, wmsFolder, i < layerPrefab.DefaultEnabledLayersMax);
                
                if (i % 10 == 0)
                    yield return null;
            }

            Destroy(coroutineRunner);
        }

        private void CreateLayer(MapFilters mapFilters, Uri url, LayerData folderLayer, bool defaultEnabled)
        {
            App.Layers.Add(
                new WmsLayerPreset.Args(url, mapFilters, folderLayer, defaultEnabled)
            );
        }
    }
}
