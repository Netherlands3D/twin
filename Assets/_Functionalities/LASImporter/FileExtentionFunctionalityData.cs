using System;
using System.Runtime.Serialization;

namespace Netherlands3D.Twin.Functionalities
{
    [Serializable]
    [DataContract(
        Namespace = "https://netherlands3d.eu/schemas/projects/functionalities",
        Name = "FileExtension"
    )]
    public sealed class FileExtensionFunctionalityData : TypedFunctionalityData<FileExtensionFunctionalityData>
    {
        [DataMember] public string FileExtention;

        protected override FileExtensionFunctionalityData CreateTypedCopy()
        {
            return new FileExtensionFunctionalityData
            {
                Id = Id,
                IsEnabled = IsEnabled,
                FileExtention = FileExtention
            };
        }
    }
}