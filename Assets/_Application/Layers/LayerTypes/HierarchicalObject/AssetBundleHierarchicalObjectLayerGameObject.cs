using System;
using UnityEngine;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using System.Collections.Generic;
using Netherlands3D.Functionalities.AssetBundles;
using Netherlands3D.Services;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    //for more info on how to make an assetbundle go to the assetbundleservice
    public class AssetBundleHierarchicalObjectLayerGameObject : HierarchicalObjectLayerGameObject
    {
        private AssetBundleService assetBundleService;
        private string bundleName;
        private string prefabName;
        private GameObject instantiatedPrefab;
        private int loadVersion;
        private SpawnFromCoordinate fromCoordinate;

        protected override void OnVisualizationInitialize()
        {
            base.OnVisualizationInitialize();
            assetBundleService = ServiceLocator.GetService<AssetBundleService>();
        }

        protected override void OnVisualizationReady()
        {
            base.OnVisualizationReady();
            LayerURLPropertyData streamingAssetsUrl = LayerData.GetProperty<LayerURLPropertyData>();
            (string bundleName, string prefabName) = assetBundleService.ParseAssetBundleUri(streamingAssetsUrl.Url);
            this.bundleName = bundleName;
            this.prefabName = prefabName;
            Load();
        }

        public override void OnSelect(LayerData layer)
        {
            //this is to prevent executing base class functionality
        }

        public override void OnDeselect(LayerData layer)
        {
            //this is to prevent executing base class functionality
        }

        private void Load()
        {
            if (instantiatedPrefab != null)
                return;

            assetBundleService.InstantiatePrefab(bundleName, prefabName, transform, instance =>
            {
                instantiatedPrefab = instance;
                instantiatedPrefab.transform.localPosition = Vector3.zero;
                fromCoordinate = instance.GetComponent<SpawnFromCoordinate>();
                var transformProperty = LayerData.GetProperty<TransformLayerPropertyData>();
                Coordinate coordinate = fromCoordinate.Coordinate.Convert(CoordinateSystems.connectedCoordinateSystem);
                transformProperty.EulerRotation = fromCoordinate.Rotation;
                transformProperty.LocalScale = transform.localScale;
                transformProperty.Position = coordinate;
                GetComponent<WorldTransform>().MoveToCoordinate(coordinate);
                
            });
        }

        private void Unload()
        {
            if (instantiatedPrefab == null)
                return;

            Destroy(instantiatedPrefab);
            instantiatedPrefab = null;
        }

        private void OnDestroy()
        {
            Unload();
        }
    }
}