using System;
using System.IO;
using CsvHelper;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.DataTypeAdapters
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/CSVImportAdapter", fileName = "CSVImportAdapter", order = 0)]
    public class SubObjectColorCSVImportAdapter : ScriptableObject, IDataTypeAdapter<LayerPresetArgs>
    {
        [SerializeField] private CartesianTileSubObjectColorLayerGameObject layerGameObjectPrefab;
        [SerializeField] private UnityEvent<float> progressEvent = new();
        public bool Supports(LocalFile localFile)
        {
            try
            {
                return new CartesianTileSubObjectColorCsv(localFile.LocalFilePath).IsValid();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CsvHelperException)
            {
                return false;
            }
        }

        public LayerPresetArgs Execute(LocalFile localFile)
        {
            if (!FileTypeAdapter.CanImportCsv())
                return null;

            var prefabIdentifier = layerGameObjectPrefab ? layerGameObjectPrefab.PrefabIdentifier : null;
            if (string.IsNullOrEmpty(prefabIdentifier))
            {
                App.Snackbar.DisplayError("De laag voor gebouwkleuren kan niet worden toegevoegd.");
                return null;
            }

            var propertyData = new CartesianTileSubObjectColorPropertyData
            {
                Data = AssetUriFactory.CreateProjectAssetUri(localFile.LocalFilePath)
            };

            try
            {
                propertyData.LoadFromProjectAsset();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CsvHelperException or FormatException)
            {
                App.Snackbar.DisplayError("Dit CSV-bestand met gebouwkleuren kan niet worden ingeladen.");
                return null;
            }

            if (propertyData.Colors.Count == 0)
            {
                App.Snackbar.DisplayError("Dit CSV-bestand bevat geen geldige gebouwkleuren.");
                return null;
            }

            foreach (var layer in ProjectData.Current.RootLayer.GetFlatHierarchy())
            {
                if (!layer.IsDisposed && layer.GetProperty<CartesianTileSubObjectColorPropertyData>() != null)
                    App.Layers.Remove(layer);
            }

            progressEvent.Invoke(1f);
            return new BuildingColorCsvPreset.Args(localFile.FileName, prefabIdentifier, propertyData);
        }
    }
}
