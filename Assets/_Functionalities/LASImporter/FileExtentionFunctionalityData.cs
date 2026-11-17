using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Netherlands3D.Twin.Functionalities
{
    [Serializable]
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/functionalities", Name = "FileExtension")]
    public sealed class FileExtensionFunctionalityData : TypedFunctionalityData<FileExtensionFunctionalityData>
    {
        //todo in the future this should be seperated when tools and functionalities will be unified
        public string FileExtention;

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