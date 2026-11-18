using System;
using UnityEngine;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using System.Collections.Generic;
using Netherlands3D.Functionalities.AssetBundles;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    //on how to make an assetbundle go to the assetbundleservice for info
    public class AssetBundleLayerGameObject : HierarchicalObjectLayerGameObject
    {
        private AssetBundleService assetBundleService;
        private string bundleName;
        private string prefabName;

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
        
        private GameObject instantiatedPrefab;
        private int loadVersion;

        private void Load()
        {
            if (instantiatedPrefab != null)
                return;

            assetBundleService.InstantiatePrefab(bundleName, prefabName, transform, instance =>
                {
                    instantiatedPrefab = instance;
                    instantiatedPrefab.transform.localPosition = Vector3.zero;
                    fromCoordinate = instance.GetComponent<SpawnFromCoordinate>();
                });
        }
        
        protected override void UpdatePosition(Coordinate newPosition)
        {
            if (fromCoordinate == null)
            {
                base.UpdatePosition(newPosition);
                return;
            }
            
            var transformProperty = LayerData.GetProperty<TransformLayerPropertyData>();
            transformProperty.Position = fromCoordinate.Coordinate.Convert(CoordinateSystems.connectedCoordinateSystem);
            transformProperty.EulerRotation = new Vector3(0, fromCoordinate.Rotation, 0);
            transformProperty.LocalScale = transform.localScale;
            base.UpdatePosition(newPosition);
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