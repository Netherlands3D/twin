using System.Collections.Generic;
using Netherlands3D.Catalogs;
using Netherlands3D.Twin.Tools;
using UnityEngine;

namespace Netherlands3D.AssetLibrary.Entries
{
    [CreateAssetMenu(menuName = "Netherlands3D/Asset Library/Tool Event")]
    public class ToolAssetEntry : AssetLibraryEntry
    {
        [Tooltip("If set, uses event:///[InstanceID]")]
        [SerializeField] private Tool tool;

        public override ICatalogItem ToCatalogItem()
        {
            if (!tool)
            {
                Debug.LogWarning("The asset library's event entry does not contain a ScriptableObject Event.", this);
                return null;
            }
            return new ToolItem(Id, Title, Description, tool);
        }
        
        public override IEnumerable<ScriptableObject> CollectEvents()
        {
            yield return tool;
        }
    }
}