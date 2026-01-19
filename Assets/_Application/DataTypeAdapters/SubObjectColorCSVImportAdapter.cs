using System.IO;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Services;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.DataTypeAdapters
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/CSVImportAdapter", fileName = "CSVImportAdapter", order = 0)]
    public class SubObjectColorCSVImportAdapter : ScriptableObject, IDataTypeAdapter<Layer>
    {
        [SerializeField] private CartesianTileSubObjectColorLayerGameObject layerGameObjectPrefab;
        [SerializeField] private UnityEvent<string> csvReplacedMessageEvent = new();
        [SerializeField] private UnityEvent<float> progressEvent = new();
        private static CartesianTileSubObjectColorLayerGameObject activeCartesianTileSubObjectColorLayer; //todo: allow multiple datasets to exist

        public bool Supports(LocalFile localFile)
        {
            return new CartesianTileSubObjectColorCsv(localFile.LocalFilePath).IsValid();
        }

        public Layer Execute(LocalFile localFile)
        {
            //todo: temp fix to allow only 1 dataset layer
            if (activeCartesianTileSubObjectColorLayer != null)
            {
                RemovePreviousColoring();
            }

            var fileName = Path.GetFileName(localFile.LocalFilePath);
            
            activeCartesianTileSubObjectColorLayer = Instantiate(layerGameObjectPrefab); //todo: replace this with App.Layers.Add
            activeCartesianTileSubObjectColorLayer.gameObject.name = fileName;
            var propertyData = activeCartesianTileSubObjectColorLayer.LayerData.GetProperty<CartesianTileSubObjectColorPropertyData>();
            propertyData.Data = AssetUriFactory.CreateProjectAssetUri(localFile.LocalFilePath);
            
            // TODO: Temporary proxying during refactoring, it would be better to simplify this.
            activeCartesianTileSubObjectColorLayer
                .progressEvent.AddListener(value => progressEvent.Invoke(value));
            return null; //todo this is now broken, should be fixed
        }

        private void RemovePreviousColoring()
        {
            activeCartesianTileSubObjectColorLayer.RemoveCustomColorSet(); //remove before destroying because otherwise the Start() function of the new colorset will apply the new colors before the OnDestroy function can clean up the old colorset. 

            activeCartesianTileSubObjectColorLayer.DestroyLayer();
            csvReplacedMessageEvent.Invoke("Het oude CSV bestand is vervangen door het nieuw gekozen CSV bestand.");
        }
    }
}