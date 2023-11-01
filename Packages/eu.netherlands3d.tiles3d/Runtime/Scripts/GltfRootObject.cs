using System;


namespace GltfMeshFeatures
{
    [Serializable]
    public class GltfRootObject
    {
        public Accessor[] accessors { get; set; }
        public Asset asset { get; set; }
        public BufferView[] bufferViews { get; set; }
        public Buffer[] buffers { get; set; }
        public Extensions extensions { get; set; }
        public string[] extensionsRequired { get; set; }
        public string[] extensionsUsed { get; set; }
        public Material[] materials { get; set; }
        public Mesh[] meshes { get; set; }
        public Node[] nodes { get; set; }
        public int scene { get; set; }
        public Scene[] scenes { get; set; }
    }

    [Serializable]
    public class Accessor
    {
        public int bufferView { get; set; }
        public int componentType { get; set; }
        public int count { get; set; }
        public double[] max { get; set; }
        public double[] min { get; set; }
        public string type { get; set; }
        public int byteOffset { get; set; }
        public bool normalized { get; set; }
    }

    [Serializable]
    public class Asset
    {
        public string generator { get; set; }
        public string version { get; set; }
    }

    [Serializable]
    public class BufferView
    {
        public int buffer { get; set; }
        public int byteLength { get; set; }
        public Extentions extensions { get; set; }
        public int byteOffset { get; set; }
        public int byteStride { get; set; }
        public int target { get; set; }
    }

    [Serializable]
    public class Buffer
    {
        public int byteLength { get; set; }
        public EXTMeshoptCompression extensions { get; set; }
    }

    [Serializable]
    public class Extentions{
        public EXTMeshoptCompression EXT_meshopt_compression { get; set; }
    }

    [Serializable]
    public class EXTMeshoptCompression
    {
        public int buffer { get; set; }
        public int byteLength { get; set; }
        public int byteOffset { get; set; }
        public int byteStride { get; set; }
        public int count { get; set; }
        public string mode { get; set; }
        public bool fallback { get; set; }
    }

    [Serializable]
    public class Extensions
    {
        public EXTStructuralMetadata EXT_structural_metadata { get; set; }
    }

    [Serializable]
    public class EXTStructuralMetadata
    {
        public PropertyTable[] propertyTables { get; set; }
        public Schema schema { get; set; }
    }

    [Serializable]
    public class PropertyTable
    {
        public string @class { get; set; }
        public int count { get; set; }
        public string name { get; set; }
        public Properties properties { get; set; }
    }

    [Serializable]
    public class Properties
    {
        public Bagpandid bagpandid { get; set; }
        public Bouwjaar bouwjaar { get; set; }
        public Objectid objectid { get; set; }
    }

    [Serializable]
    public class Bagpandid
    {
        public string stringOffsetType { get; set; }
        public int stringOffsets { get; set; }
        public int values { get; set; }
    }

    [Serializable]
    public class Bouwjaar
    {
        public int values { get; set; }
    }

    [Serializable]
    public class Objectid
    {
        public int values { get; set; }
    }

    [Serializable]
    public class Schema
    {
        public Classes classes { get; set; }
    }

    [Serializable]
    public class Classes
    {
        public Building building { get; set; }
    }

    [Serializable]
    public class Building
    {
        public string description { get; set; }
        public string name { get; set; }
        public Properties properties { get; set; }
    }

    [Serializable]
    public class Material
    {
        public PbrMetallicRoughness pbrMetallicRoughness { get; set; }
    }

    [Serializable]
    public class PbrMetallicRoughness
    {
        public double[] baseColorFactor { get; set; }
    }

    [Serializable]
    public class Mesh
    {
        public Primitive[] primitives { get; set; }
    }

    [Serializable]
    public class Primitive
    {
        public FeatureAttribute attributes { get; set; }
        public EXTMeshFeatures extensions { get; set; }
        public int indices { get; set; }
        public int material { get; set; }
        public int mode { get; set; }
    }

    [Serializable]
    public class FeatureAttribute
    {
        public int NORMAL { get; set; }
        public int POSITION { get; set; }
        public int _FEATURE_ID_0 { get; set; }
    }

    [Serializable]
    public class EXTMeshFeatures
    {
        public FeatureId[] featureIds { get; set; }
    }

    [Serializable]
    public class FeatureId
    {
        public int attribute { get; set; }
        public int featureCount { get; set; }
        public int propertyTable { get; set; }
    }

    [Serializable]
    public class Node
    {
        public double[] matrix { get; set; }
        public int mesh { get; set; }
    }

    [Serializable]
    public class Scene
    {
        public int[] nodes { get; set; }
    }
}