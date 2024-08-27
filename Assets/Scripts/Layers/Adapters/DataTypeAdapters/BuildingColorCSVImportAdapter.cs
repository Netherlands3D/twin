using System;
using System.Collections;
using System.IO;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.Events;
using Application = UnityEngine.Application;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/CSVImportAdapter", fileName = "CSVImportAdapter", order = 0)]
    public class BuildingColorCSVImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private CartesianTileBuildingColorLayerGameObject layerGameObjectPrefab;
        [SerializeField] private UnityEvent<string> csvReplacedMessageEvent = new();
        [SerializeField] private UnityEvent<float> progressEvent = new();
        private static CartesianTileBuildingColorLayerGameObject activeCartesianTileBuildingColorLayer; //todo: allow multiple datasets to exist

        public bool Supports(LocalFile localFile)
        {
            return new CartesianTileBuildingColorCsv(localFile.LocalFilePath).IsValid();
        }

        public void Execute(LocalFile localFile)
        {
            //todo: temp fix to allow only 1 dataset layer
            if (activeCartesianTileBuildingColorLayer != null)
            {
                RemovePreviousColoring();
            }

            var fullPath = localFile.LocalFilePath;
            var fileName = Path.GetFileName(fullPath);
            
            activeCartesianTileBuildingColorLayer = Instantiate(layerGameObjectPrefab);
            activeCartesianTileBuildingColorLayer.gameObject.name = fileName;
            var propertyData = activeCartesianTileBuildingColorLayer.PropertyData as CartesianTileBuildingColorPropertyData;
            propertyData.Data = new Uri("project:///" + fullPath);
            
            // TODO: Temporary proxying during refactoring, it would be better to simplify this.
            activeCartesianTileBuildingColorLayer
                .progressEvent.AddListener(value => progressEvent.Invoke(value));
        }

        private void RemovePreviousColoring()
        {
            activeCartesianTileBuildingColorLayer.RemoveCustomColorSet(); //remove before destroying because otherwise the Start() function of the new colorset will apply the new colors before the OnDestroy function can clean up the old colorset. 

            activeCartesianTileBuildingColorLayer.DestroyLayer();
            csvReplacedMessageEvent.Invoke("Het oude CSV bestand is vervangen door het nieuw gekozen CSV bestand.");
        }
    }
}