using GLTFast;
using GLTFast.Schema;
using Netherlands3D.Coordinates;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Tiles3D
{
    public class ContentMetadata : MonoBehaviour
    {
        public GltfMeshFeatures.Asset asset;

        public UnityEvent<ContentMetadata> OnDestroyed = new();

        private void OnDestroy()
        {
            OnDestroyed.Invoke(this);
            OnDestroyed.RemoveAllListeners();
        }
    }
}