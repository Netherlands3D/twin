using System.Collections.Generic;

namespace Netherlands3D.Twin.Utility
{
    public class BoundingBoxContainer
    {
        public string url;
        public BoundingBox GlobalBoundingBox;
        public Dictionary<string, BoundingBox> LayerBoundingBoxes = new();

        public BoundingBoxContainer(string url)
        {
            this.url = url;
        }
    }
}
