using System;
using System.Collections.Generic;
using Netherlands3D.Catalogs;
using Netherlands3D.Catalogs.CatalogItems;
using Netherlands3D.Events;
using UnityEngine;

namespace Netherlands3D.AssetLibrary.Entries
{
    [CreateAssetMenu(menuName = "Netherlands3D/Asset Library/ScriptableObject Event")]
    public class ScriptableObjectEventAssetEntry : AssetLibraryEntry
    {
        [Tooltip("If set, uses event:///[InstanceID]")]
        [SerializeField] private TriggerEvent scriptableObjectEvent;

        public override ICatalogItem ToCatalogItem()
        {
            if (!scriptableObjectEvent)
            {
                Debug.LogWarning("The asset library's event entry does not contain a ScriptableObject Event.", this);
                return null;
            }
            
            var resolved = $"event:///{scriptableObjectEvent.GetInstanceID()}";

            var uri = string.IsNullOrWhiteSpace(resolved) ? null : new Uri(resolved, UriKind.Absolute);
            return new DataService(Id, Title, Description, endpoint: uri);
        }
        
        public override IEnumerable<ScriptableObject> CollectEvents()
        {
            yield return scriptableObjectEvent;
        }

    }
}