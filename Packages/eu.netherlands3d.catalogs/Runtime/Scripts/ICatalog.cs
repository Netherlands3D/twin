using System.Threading.Tasks;
using Netherlands3D.SerializableGisExpressions;

namespace Netherlands3D.Catalogs
{
    public interface ICatalog : ICatalogItem
    {
        public Task<IPaginatedRecordCollection> BrowseAsync(Pagination pagination = null);
        public Task<IPaginatedRecordCollection> SearchAsync(string query, Pagination pagination = null);
        public Task<IPaginatedRecordCollection> SearchAsync(Expression expression, Pagination pagination = null);
    }
}