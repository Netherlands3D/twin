using System.IO;
using UnityEngine;
using UnityEngine.Events;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;

namespace Netherlands3D.Twin.DataTypeAdapters
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/GLBImportAdapter", fileName = "GLBImportAdapter", order = 0)]
    public class GLBImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private GeoJsonLayerGameObject layerPrefab;
        [SerializeField] private UnityEvent<string> displayErrorMessageEvent;

        public bool Supports(LocalFile localFile)
        {
            try
            {
                using (var fs = new FileStream(localFile.LocalFilePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] header = new byte[4];
                    int bytesRead = fs.Read(header, 0, 4);

                    // Check for "glTF" ASCII signature
                    return bytesRead == 4 &&
                           header[0] == 0x67 && // 'g'
                           header[1] == 0x6C && // 'l'
                           header[2] == 0x54 && // 'T'
                           header[3] == 0x46;   // 'F'
                }
            }
            catch
            {
                return false;
            } 
        }

        public void Execute(LocalFile localFile)
        {
            Debug.LogError("Parse GLB");
//            CreateGeoJSONLayer(localFile, displayErrorMessageEvent);
        }

        private void CreateGeoJSONLayer(LocalFile localFile, UnityEvent<string> onErrorCallback = null)
        {
            var localFilePath = Path.Combine(Application.persistentDataPath, localFile.LocalFilePath);
            var geoJsonLayerName = Path.GetFileName(localFile.SourceUrl);
            if(localFile.SourceUrl.Length > 0)
                geoJsonLayerName = localFile.SourceUrl;    
        
            GeoJsonLayerGameObject newLayer = Instantiate(layerPrefab);
            newLayer.Name = geoJsonLayerName;
            newLayer.gameObject.name = geoJsonLayerName;
            if (onErrorCallback != null)
                newLayer.Parser.OnParseError.AddListener(onErrorCallback.Invoke);

            //GeoJSON layer+visual colors are set to random colors until user can pick colors in UI
            var randomLayerColor = Color.HSVToRGB(UnityEngine.Random.value, UnityEngine.Random.Range(0.5f, 1f), 1);
            randomLayerColor.a = 0.5f;
            newLayer.LayerData.Color = randomLayerColor;
            
            var symbolizer = newLayer.LayerData.DefaultSymbolizer;
            symbolizer?.SetFillColor(randomLayerColor);
            symbolizer?.SetStrokeColor(randomLayerColor);
            
            var localPath = localFile.LocalFilePath;
            var propertyData = newLayer.PropertyData as LayerURLPropertyData;
            propertyData.Data = localFile.SourceUrl.StartsWith("http") 
                ? AssetUriFactory.CreateRemoteAssetUri(localFile.SourceUrl) 
                : AssetUriFactory.CreateProjectAssetUri(localPath);
        }
    }
}