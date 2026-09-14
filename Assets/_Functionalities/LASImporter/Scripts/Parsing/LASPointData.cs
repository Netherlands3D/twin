using UnityEngine;

namespace Netherlands3D.Functionalities.LASImporter.Parsing
{
    public readonly struct LASPointData
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;
        public readonly byte Classification;
        public readonly Color32 Color;
        public readonly bool HasColor;

        public LASPointData(double x, double y, double z, byte classification, Color32 color, bool hasColor)
        {
            X = x;
            Y = y;
            Z = z;
            Classification = classification;
            Color = color;
            HasColor = hasColor;
        }
    }
}
