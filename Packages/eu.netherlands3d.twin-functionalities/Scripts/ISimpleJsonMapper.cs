using SimpleJSON;

namespace Netherlands3D.Twin.Functionalities
{
    public interface ISimpleJsonMapper
    {
        public void Populate(JSONNode jsonNode);
        public JSONNode ToJsonNode();
    }
}