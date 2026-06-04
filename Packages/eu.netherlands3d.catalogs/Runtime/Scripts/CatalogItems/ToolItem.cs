using Netherlands3D.Catalogs.CatalogItems;
using UnityEngine;

namespace Netherlands3D.Catalogs
{
    public record ToolItem : BaseCatalogItem
    {
        public ScriptableObject Tool { get; private set; }
        
        public ToolItem(
            string id,
            string title,
            string description,
            ScriptableObject tool,
            bool withoutNotify = false
        ) : base(id, title, description)
        {
            Tool = tool;
        }
    }
}
