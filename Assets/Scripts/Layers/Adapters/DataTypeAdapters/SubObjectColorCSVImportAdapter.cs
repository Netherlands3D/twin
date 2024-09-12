using System;
using System.Collections;
using System.IO;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;
using UnityEngine.Events;
using Application = UnityEngine.Application;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/CSVImportAdapter", fileName = "CSVImportAdapter", order = 0)]
    public class SubObjectColorCSVImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private CartesianTileSubObjectColorLayerGameObject layerGameObjectPrefab;
        [SerializeField] private UnityEvent<string> csvReplacedMessageEvent = new();
        [SerializeField] private UnityEvent<float> progressEvent = new();
        private static CartesianTileSubObjectColorLayerGameObject activeCartesianTileSubObjectColorLayer; //todo: allow multiple datasets to exist

        public bool Supports(LocalFile localFile)
        {
            return new CartesianTileSubObjectColorCsv(localFile.LocalFilePath).IsValid();
        }

        public void Execute(LocalFile localFile)
        {
            //todo: temp fix to allow only 1 dataset layer
            if (activeCartesianTileSubObjectColorLayer != null)
            {
                RemovePreviousColoring();
            }

            var fullPath = localFile.LocalFilePath;
            var fileName = Path.GetFileName(fullPath);
            
            activeCartesianTileSubObjectColorLayer = Instantiate(layerGameObjectPrefab);
            activeCartesianTileSubObjectColorLayer.gameObject.name = fileName;
            var propertyData = activeCartesianTileSubObjectColorLayer.PropertyData as CartesianTileSubObjectColorPropertyData;
            propertyData.Data = AssetUriFactory.CreateProjectAssetUri(fullPath);
            
            // TODO: Temporary proxying during refactoring, it would be better to simplify this.
            activeCartesianTileSubObjectColorLayer
                .progressEvent.AddListener(value => progressEvent.Invoke(value));
        }

        private void RemovePreviousColoring()
        {
            activeCartesianTileSubObjectColorLayer.RemoveCustomColorSet(); //remove before destroying because otherwise the Start() function of the new colorset will apply the new colors before the OnDestroy function can clean up the old colorset. 

            activeCartesianTileSubObjectColorLayer.DestroyLayer();
            csvReplacedMessageEvent.Invoke("Het oude CSV bestand is vervangen door het nieuw gekozen CSV bestand.");
        }
    }
}