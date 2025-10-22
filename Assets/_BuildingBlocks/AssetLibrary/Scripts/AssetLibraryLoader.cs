using System.Collections;
using UnityEngine;

namespace Netherlands3D.AssetLibrary
{
    public class AssetLibraryLoader : MonoBehaviour
    {
        public AssetLibrary assetLibrary;

        private IEnumerator Start()
        {
            // Skip a frame before loading the asset catalog to make sure initialisation of the app is complete and
            // to spread the load
            yield return null;

            InitializeAssetLibrary();
        }

        private async void InitializeAssetLibrary()
        {
            await assetLibrary.Initialize();
        }
    }
}
