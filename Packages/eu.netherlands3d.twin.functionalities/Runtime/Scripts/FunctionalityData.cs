using System;
using System.Runtime.Serialization;

namespace Netherlands3D.Twin.Functionalities
{
    [Serializable]
    [DataContract(
        Namespace = "https://netherlands3d.eu/schemas/projects/functionalities",
        Name = "Functionality")]
    public class FunctionalityData
    {
        [DataMember] public string Id;
        [DataMember] public bool IsEnabled;

        internal virtual FunctionalityData CreateCopy()
        {
            return new FunctionalityData
            {
                Id = Id,
                IsEnabled = IsEnabled
            };
        }

        public override bool Equals(object obj)
        {
            if (obj is not FunctionalityData functionalityData)
                return false;

            return functionalityData.Id == Id;
        }

        protected bool Equals(FunctionalityData other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }
    }
}