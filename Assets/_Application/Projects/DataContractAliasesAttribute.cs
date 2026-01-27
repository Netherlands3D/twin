using System;

namespace Netherlands3D.Twin.Projects
{
    /// <summary>
    /// Allow for backwards compatibility when merging classes together - this way you can declare one or more aliases for a
    /// data type. This will ensure the namespace is the same for all aliases, we assume that you do not want to merge objects
    /// in different namespaces.
    ///
    /// See DataContract attribute and DataContractSerializationBinder for more details.
    /// </summary>
    public class DataContractAliasesAttribute : Attribute
    {
        /// <summary>
        /// Namespace of the alias to track.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// All aliases to collapse onto the existing DataContract attribute.
        /// </summary>
        public string[] Names { get; set; }
    }
}