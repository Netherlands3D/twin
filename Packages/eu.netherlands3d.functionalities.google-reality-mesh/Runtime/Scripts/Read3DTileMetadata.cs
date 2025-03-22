using System.Collections.Generic;
using Netherlands3D.Tiles3D;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Functionalities.GoogleRealityMesh
{
    public class Read3DTileMetadata : MonoBehaviour
    {
        [SerializeField] private Read3DTileset tilesetReader;
        public UnityEvent<string> onReadCopyright = new();

        private List<ContentMetadata> allMetadata = new();

        [SerializeField] private string splitCopyrightCharacter = ";";
        public string SplitCopyrightCharacter { get => splitCopyrightCharacter; set => splitCopyrightCharacter = value; }

        private void OnEnable() {
            tilesetReader.OnLoadAssetMetadata.AddListener(OnLoadAssetMetaData);
        }

        private void OnDisable() {
            tilesetReader.OnLoadAssetMetadata.RemoveListener(OnLoadAssetMetaData);
        }

        private void OnLoadAssetMetaData(ContentMetadata assetMetadata)
        {
            assetMetadata.OnDestroyed.AddListener(RemoveMetadata);

            if(!allMetadata.Contains(assetMetadata))
                allMetadata.Add(assetMetadata);

            FilterChangedMetadata();
        }

        private void RemoveMetadata(ContentMetadata assetMetadata)
        {
            if(allMetadata.Contains(assetMetadata))
                allMetadata.Remove(assetMetadata);

            FilterChangedMetadata();
        }

        /// <summary>
        /// Filter all metadata for unique copyrights (Google seperates multiple coprights in rootJson.asset.copyright using ; character)
        /// </summary>
        private void FilterChangedMetadata()
        {
            string combinedCopyrightOutput = "";

            //Sort allMetadata by most copyright occurances
            allMetadata.Sort((a, b) => a.asset?.copyright.CompareTo(b.asset?.copyright) ?? 0);

            List<string> uniqueCopyrights = new();
            foreach (var metadata in allMetadata)
            {
                if (!string.IsNullOrEmpty(metadata.asset?.copyright))
                {
                    var split = metadata.asset.copyright.Split(SplitCopyrightCharacter);
                    foreach (var copyright in split)
                    {
                        if (!uniqueCopyrights.Contains(copyright))
                            uniqueCopyrights.Add(copyright);
                    }
                }
            }              
            for (int i = 0; i < uniqueCopyrights.Count; i++)
            {
                combinedCopyrightOutput += uniqueCopyrights[i];
                if (i < uniqueCopyrights.Count - 1)
                    combinedCopyrightOutput += SplitCopyrightCharacter;
            }

            onReadCopyright.Invoke(combinedCopyrightOutput);
        }
    }
}
