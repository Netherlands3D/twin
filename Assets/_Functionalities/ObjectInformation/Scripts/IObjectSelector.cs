using System.Collections.Generic;

namespace Netherlands3D.Functionalities.ObjectInformation
{
    public interface IObjectSelector
    {
        void TryAddMappings(Dictionary<IMapping, int> mappings, bool filterDuplicateFeatures);
    }
}