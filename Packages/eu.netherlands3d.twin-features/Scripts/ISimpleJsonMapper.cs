using SimpleJSON;

namespace Netherlands3D.Twin.Features
{
    public interface ISimpleJsonMapper
    {
        public void Populate(JSONNode jsonNode);
        public JSONNode ToJsonNode();
    }
}