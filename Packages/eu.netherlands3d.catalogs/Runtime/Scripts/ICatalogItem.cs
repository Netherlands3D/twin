using System;
using System.Collections.Generic;

namespace Netherlands3D.Catalogs
{
    /// <summary>
    /// Something you can browse in a catalog: either a real data record,
    /// a folder (with its own children), or a link to another catalog.
    /// </summary>
    public interface ICatalogItem
    {
        string Id { get; }
        string Title { get; }
        string Description { get; }
        IDictionary<string, object> Metadata { get; }
    }
}