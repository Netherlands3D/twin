using System;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Functionalities;

namespace Netherlands3D.Twin.PresentationModus
{
    [Serializable]
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/functionalities", Name = "PresentationMode")]
    public sealed class PresentationModeFunctionalityData : TypedFunctionalityData<PresentationModeFunctionalityData>
    {
        [DataMember] public bool PresentationLeftPinned;
        [DataMember] public bool PresentationToolboxPinned;
        [DataMember] public bool PresentationNavigationPinned;
        
        protected override PresentationModeFunctionalityData CreateTypedCopy()
        {
            return new PresentationModeFunctionalityData
            {
                Id = Id,
                IsEnabled = IsEnabled,
                PresentationLeftPinned = PresentationLeftPinned,
                PresentationToolboxPinned = PresentationToolboxPinned,
                PresentationNavigationPinned = PresentationNavigationPinned
            };
        }
    }
}