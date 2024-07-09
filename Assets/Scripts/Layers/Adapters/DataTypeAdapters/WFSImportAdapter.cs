using System.IO;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/WFSImportAdapter", fileName = "WFSImportAdapter", order = 0)]
    public class WFSImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        public bool Supports(LocalFile localFile)
        {
            if(!localFile.SourceUrl.ToLower().Contains("request=getcapabilities") || localFile.SourceUrl.ToLower().Contains("request=getfeature"))
                return false;

            var cachedDataPath = localFile.LocalFilePath;

            //Read all text from file
            var fileData = File.ReadAllText(cachedDataPath);

            return false;
        }

        public void Execute(LocalFile localFile)
        {
            // GetCapabilities? Retrieve all possible feature types
            // GetFeature? Retrieve specific feature type

            // Construct specific bbox query URL's from source url for CartesianTiles layer
            // Generate the layer
        }
    }
}
