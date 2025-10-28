using SimpleJSON;
using System.Collections.Generic;

namespace Netherlands3D.CityJson.Structure
{
    /// <summary>
    /// CityJSON Appearance object according to v1.0.3 spec
    /// https://www.cityjson.org/specs/1.0.3/#appearance-object
    /// </summary>
    [System.Serializable]
    public class CityAppearance
    {
        public List<CityMaterialInfo> MaterialInfos { get; private set; } = new List<CityMaterialInfo>();
        public List<CityTextureInfo> TextureInfos { get; private set; } = new List<CityTextureInfo>();
        // Todo: Add vertices-texture support
        // Todo: Add default-theme-texture support
        // Todo: Add default-theme-material support

        public static CityAppearance FromJSON(JSONNode node)
        {
            var appearance = new CityAppearance();

            var materialsNode = node["materials"];

            for (var i = 0; i < materialsNode.Count; i++)
            {
                var matNode = materialsNode[i];
                var materialInfo = CityMaterialInfo.FromJSON(matNode);
                appearance.MaterialInfos.Add(materialInfo);
            }

            var texturesNode = node["textures"];
            if (texturesNode != null && texturesNode.IsArray)
            {
                foreach (var texNode in texturesNode.AsArray)
                {
                    appearance.TextureInfos.Add(CityTextureInfo.FromJSON(texNode));
                }
            }

            return appearance;
        }
    }
}