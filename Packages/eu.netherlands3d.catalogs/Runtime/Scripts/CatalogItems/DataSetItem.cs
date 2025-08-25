using System.Collections.Generic;
using System.Threading.Tasks;
using Netherlands3D.SerializableGisExpressions;

namespace Netherlands3D.Catalogs.CatalogItems
{
    /// <summary>
    /// A DataSet in the catalog that contains its own paginated children. A DataSet is a single 'theme' -such as
    /// parking spots in Amsterdam- with multiple 'distributions' - meaning it can have a WFS, WMS, WMTS, download or
    /// any other form of record available that presents the same dataset.
    /// 
    /// Implements IPaginatedRecordCollection so you can directly page through it.
    /// </summary>
    public record DataSetItem : BaseCatalogItemWithChildren
    {
        public DataSetItem(
            string id,
            string title,
            string description,
            ICatalogItemCollection children
        ) : base(id, title, description, children)
        {
        }
    }
}