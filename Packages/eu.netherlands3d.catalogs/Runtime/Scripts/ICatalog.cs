using System.Threading.Tasks;
using Netherlands3D.SerializableGisExpressions;

namespace Netherlands3D.Catalogs
{
    public interface ICatalog
    {
        public Task<IRecordCollection> BrowseAsync(int limit = 50, int offset = 0);
        public Task<IRecordCollection> SearchAsync(Expression expression, int limit = 50, int offset = 0);
    }
}