using System;

namespace Netherlands3D.Twin.Functionalities
{
    [Serializable]
    public class FunctionalityData
    {
        public string Id;
        public bool IsEnabled;

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
