using System.Collections.Generic;
using System.IO;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using Netherlands3D.Coordinates;
using System.Runtime.InteropServices;

namespace Netherlands3D.Dxf
{
    public class DxfFile
    {
        [DllImport("__Internal")]
        private static extern void DownloadFile(string callbackGameObjectName, string callbackMethodName, string fileName, byte[] array, int byteLength);

        private DxfDocument dxfDocument;
        private Layer dxfLayer;

        public void SetupDXF()
        {
            dxfDocument = new DxfDocument();
            dxfDocument.DrawingVariables.InsUnits = netDxf.Units.DrawingUnits.Meters;
        }

        public void AddLayer(List<Vector3RD> triangleVertices, string layerName, AciColor layerColor)
        {
            // TODO 
            // check if there are 3 triangles or less, if that is the case a polyfaceMesh cannot be built, seperate triangles have to be added to the dxf.
            dxfLayer = new Layer(layerName);
            dxfLayer.Color = layerColor;
            dxfDocument.Layers.Add(dxfLayer);

            AddMesh(triangleVertices, layerName);
        }

        public bool Save(string path)
        {
            return dxfDocument.Save(path, true);
        }

        //used in if WEBGL pragma in DXFCreation
        public bool Save(MemoryStream stream)
        {
            return dxfDocument.Save(stream);
        }

        private void AddMesh(List<Vector3RD> triangleVertices, string layerName)
        {
            PolyfaceMesh pfm;
            // create Mesh
            List<PolyfaceMeshVertex> pfmVertices = new List<PolyfaceMeshVertex>();
            pfmVertices.Capacity = triangleVertices.Count;
            List<PolyfaceMeshFace> pfmFaces = new List<PolyfaceMeshFace>();
            pfmFaces.Capacity = triangleVertices.Count / 3;
            int facecounter = 0;
            int vertexIndex = 0;

            for (int i = 0; i < triangleVertices.Count; i += 3)
            {
                pfmVertices.Add(new PolyfaceMeshVertex(triangleVertices[i].x, triangleVertices[i].y, triangleVertices[i].z));
                pfmVertices.Add(new PolyfaceMeshVertex(triangleVertices[i + 2].x, triangleVertices[i + 2].y, triangleVertices[i + 2].z));
                pfmVertices.Add(new PolyfaceMeshVertex(triangleVertices[i + 1].x, triangleVertices[i + 1].y, triangleVertices[i + 1].z));

                PolyfaceMeshFace pfmFace = new PolyfaceMeshFace(new List<short>() { (short)(vertexIndex + 1), (short)(vertexIndex + 2), (short)(vertexIndex + 3) });
                vertexIndex += 3;
                pfmFaces.Add(pfmFace);
                facecounter++;
                if (facecounter % 10000 == 0)
                {
                    pfm = new PolyfaceMesh(pfmVertices, pfmFaces);
                    pfm.Layer = dxfLayer;
                    dxfDocument.AddEntity(pfm);
                    pfmVertices.Clear();
                    pfmFaces.Clear();
                    facecounter = 0;
                    vertexIndex = 0;
                }
            }

            if (pfmFaces.Count > 0)
            {
                pfm = new PolyfaceMesh(pfmVertices, pfmFaces);
                pfm.Layer = dxfLayer;
                dxfDocument.AddEntity(pfm);
            }
        }
    }
}