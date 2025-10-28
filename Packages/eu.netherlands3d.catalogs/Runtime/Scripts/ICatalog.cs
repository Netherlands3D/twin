using System.Threading.Tasks;
using Netherlands3D.SerializableGisExpressions;

namespace Netherlands3D.Catalogs
{
    public interface ICatalog : ICatalogItem
    {
        public Task<ICatalogItemCollection> BrowseAsync(Pagination pagination = null);
        public Task<ICatalogItemCollection> SearchAsync(string query, Pagination pagination = null);
        public Task<ICatalogItemCollection> SearchAsync(Expression expression, Pagination pagination = null);
    }
}