using System;
using System.Runtime.Serialization;

namespace Netherlands3D.Twin.Functionalities
{
    /// <summary>
    /// Base class for functionality-specific project data.
    /// Derived classes must explicitly define how their defaults are copied,
    /// and implement [DataContract].
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// [Serializable]
    /// [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/functionalities", Name = "Example")]
    /// public sealed class ExampleFunctionalityData : TypedFunctionalityData<ExampleFunctionalityData> 
    /// {
    ///     [DataMember] public int ExampleValue;
    ///
    ///     protected override ExampleFunctionalityData CreateTypedCopy()
    ///     {
    ///         return new ExampleFunctionalityData
    ///         {
    ///             Id = Id,
    ///             IsEnabled = IsEnabled,
    ///             ExampleValue = ExampleValue
    ///         };
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    /// 
    [Serializable]
    [DataContract]
    public abstract class TypedFunctionalityData<TData> : FunctionalityData where TData : TypedFunctionalityData<TData>
    {
        internal sealed override FunctionalityData CreateCopy()
        {
            return CreateTypedCopy();
        }

        protected abstract TData CreateTypedCopy();

    }
}