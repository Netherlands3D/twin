using System;

namespace Netherlands3D.ObjImporter.General.GameObjectDataSet
{
    [Serializable]
    public class SubMeshData
    {
        public long vertexOffset = 0;
        public long startIndex;
        public long Indexcount;
        public string materialname;
    }

}