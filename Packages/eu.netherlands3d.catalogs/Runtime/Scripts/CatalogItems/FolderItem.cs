using System;

namespace Netherlands3D.Catalogs.CatalogItems
{
    /// <summary>
    /// A folder in the catalog that contains its own paginated children.
    /// Implements IPaginatedRecordCollection so you can directly page through it.
    /// </summary>
    public record FolderItem : BaseCatalogItemWithChildren
    {
        public FolderItem(
            string id,
            string title,
            string description,
            ICatalogItemCollection children
        ) : base(id, title, description, children)
        {
        }
    }
}