using SimpleJSON;

namespace Netherlands3D.CityJson.Structure
{
    [System.Serializable]
    public class CityTextureInfo
    {
        public string Type { get; private set; }
        public string Image { get; private set; }
        public string WrapMode { get; private set; }
        public string TextureType { get; private set; }

        public static CityTextureInfo FromJSON(JSONNode node)
        {
            if (node == null) return null;

            return new CityTextureInfo
            {
                Type = node["type"],
                Image = node["image"],
                WrapMode = node["wrapMode"],
                TextureType = node["textureType"]
            };
        }
    }
}
