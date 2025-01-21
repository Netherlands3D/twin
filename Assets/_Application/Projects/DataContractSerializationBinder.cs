using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json.Serialization;

namespace Netherlands3D.Twin.Projects
{
    /// <summary>
    /// For Netherlands3D, we want to make sure that changes in the structure of the code won't affect how types
    /// are linked in the serialized file. By default, JSON.net will include a type reference using the Assembly name
    /// and Class name (including namespace) in the serialized JSON; but this will break if we were to move classes to
    /// another assembly (such as packaging a building block or functionality) or when we need to change the namespace
    /// of a class during refactoring
    ///
    /// This Serialization Binder will ensure that if a DataContract attribute is present with a Data object -which is
    /// recommended for Netherlands3D objects- that the name and namespace with that attribute is used to populate the
    /// `$type` field in the JSON output. This will detach the name of the data object with the name of the class and
    /// give more freedom to change the internals of a building block or functionanality.
    ///
    /// This serialization binder will act as a decorator (https://refactoring.guru/design-patterns/decorator) around
    /// another SerializationBinder. When there is no DataContract attribute defined, the decorated Serialization Binder
    /// is invoked. Generally you provide the DefaultSerializationBinder by Newtonsoft, so that regular classes (such
    /// as the Unity color, Vector3 or others) still correctly serialize.
    /// </summary>
    public class DataContractSerializationBinder: ISerializationBinder
    {
        private readonly ISerializationBinder decoratedSerializationBinder;
        private IDictionary<string, Type> KnownTypes { get; set; } = new Dictionary<string, Type>();

        public DataContractSerializationBinder(ISerializationBinder decoratedSerializationBinder)
        {
            this.decoratedSerializationBinder = decoratedSerializationBinder;
            IndexAliasesForWellDefinedDataObjects();
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            var typeCodeAndType = KnownTypes.FirstOrDefault(t => t.Key == typeName);
            if (typeCodeAndType.Equals(default(KeyValuePair<string, Type>)))
            {
                return decoratedSerializationBinder.BindToType(assemblyName, typeName);
            }

            return typeCodeAndType.Value;
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            var typeCodeAndType = KnownTypes.FirstOrDefault(kv => kv.Value == serializedType);
            if (typeCodeAndType.Equals(default(KeyValuePair<string, Type>)))
            {
                decoratedSerializationBinder.BindToName(serializedType, out assemblyName, out typeName);
                return;
            }
            
            var typeCode = typeCodeAndType.Key;
            
            assemblyName = null;
            typeName = typeCode;
        }

        private void IndexAliasesForWellDefinedDataObjects()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                IndexAllTypesWithADataContract(assembly);
            }
        }

        private void IndexAllTypesWithADataContract(Assembly assembly)
        {
            var types = assembly.GetTypes().Where(t => t.IsDefined(typeof(DataContractAttribute)));
            foreach (var type in types)
            {
                IndexTypeWithDataContract(type);
            }
        }

        private void IndexTypeWithDataContract(Type type)
        {
            var attribute = Attribute.GetCustomAttribute(type, typeof(DataContractAttribute)) as DataContractAttribute;
            if (attribute == null) return;

            KnownTypes.TryAdd(ExtractTypeAlias(type, attribute), type);
        }

        private static string ExtractTypeAlias(Type type, DataContractAttribute attribute)
        {
            // By default, we assume DataContract doesn't have additional info defined and we use the Type's
            // full name as a type code
            string alias = type.FullName ?? type.Name;

            // If there is no name associated with the DataContract, that's OK and we return the type's name.
            if (string.IsNullOrEmpty(attribute.Name)) return alias;
            
            // if DataContract does have a name defined; we use that so that we type-map the data and prevent future
            // issues when changing the location or namespace of a serialized class.
            alias = attribute.Name;
            
            if (string.IsNullOrEmpty(attribute.Namespace)) return alias;

            // It would be even better if the DataContract has a vendor specific namespace, to prevent naming clashes
            return $"{attribute.Namespace}/{alias}";
        }
    }
}