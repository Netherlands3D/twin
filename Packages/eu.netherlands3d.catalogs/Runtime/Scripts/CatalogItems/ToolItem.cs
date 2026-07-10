using Netherlands3D.Catalogs.CatalogItems;
using UnityEngine;

namespace Netherlands3D.Catalogs
{
    public record ToolItem : BaseCatalogItem
    {
        public ScriptableObject ScriptableToolObject { get; private set; }
        
        public ToolItem(
            string id,
            string title,
            string description,
            ScriptableObject tool
        ) : base(id, title, description)
        {
            ScriptableToolObject = tool;
        }
    }
}
