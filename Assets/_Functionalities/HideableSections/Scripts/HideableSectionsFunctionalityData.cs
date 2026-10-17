using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Functionalities;
using UnityEngine;

namespace Netherlands3D.Twin.HideableSections
{
    [Serializable]
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/functionalities", Name = "HideableSections")]
    public sealed class HideableSectionsFunctionalityData : TypedFunctionalityData<HideableSectionsFunctionalityData>
    {
        [SerializeField] [DataMember(Name = "UnpinnedSectionIds")] private List<string> unpinnedSectionIds = new();
        [field: NonSerialized] public event Action<string, bool> PinStateChanged;

        public bool IsPinned(string sectionId)
        {
            return !unpinnedSectionIds.Contains(sectionId);
        }

        public void SetPinned(string sectionId, bool isPinned)
        {
            if (IsPinned(sectionId) == isPinned)
            {
                return;
            }
            
            if (isPinned)
            {
                while (unpinnedSectionIds.Remove(sectionId))
                {
                }
            }
            else
            {
                unpinnedSectionIds.Add(sectionId);
            }
            
            PinStateChanged?.Invoke(sectionId, isPinned);
            
        }
        
        protected override HideableSectionsFunctionalityData CreateTypedCopy()
        {
            return new HideableSectionsFunctionalityData
            {
                Id = Id,
                IsEnabled = IsEnabled,
                unpinnedSectionIds = new List<string>(unpinnedSectionIds)
            };
        }
    }
}