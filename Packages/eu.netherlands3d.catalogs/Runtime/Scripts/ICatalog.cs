using System.Threading.Tasks;
using Netherlands3D.SerializableGisExpressions;

namespace Netherlands3D.Catalogs
{
    public interface ICatalog : ICatalogItem
    {
        public Task<IPaginatedRecordCollection> BrowseAsync(int limit = 50, int offset = 0);
        public Task<IPaginatedRecordCollection> SearchAsync(string query, int limit = 50, int offset = 0);
        public Task<IPaginatedRecordCollection> SearchAsync(Expression expression, int limit = 50, int offset = 0);
    }
}