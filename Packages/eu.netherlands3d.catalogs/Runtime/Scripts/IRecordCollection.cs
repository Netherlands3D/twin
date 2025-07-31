using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netherlands3D.Catalogs
{
    public interface IRecordCollection
    {
        public Task<IEnumerable<ICatalogItem>> GetItemsAsync();
    }
}