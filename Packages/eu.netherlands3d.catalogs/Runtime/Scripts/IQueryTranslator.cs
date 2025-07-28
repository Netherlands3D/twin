using Netherlands3D.SerializableGisExpressions;

namespace Netherlands3D.Catalogs
{
    public interface IQueryTranslator
    {
        public string ToQuery(Expression expr);
    }
}