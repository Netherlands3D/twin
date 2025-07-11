using UnityEngine;
using System.IO;

namespace Netherlands3D.ObjImporter.ParseOBJ
{
    public class intList
    {
        BinaryWriter writer;
        FileStream fs;
        BinaryReader bReader;
        string datapath = "";
        string filepath;
        public void SetupWriting(string name)
        {
            if (datapath=="")
            {
                datapath = Application.persistentDataPath;
            }
            filepath = datapath + "/" + name + ".dat";
            writer = new BinaryWriter(File.Open(filepath, FileMode.OpenOrCreate));
        }
        public void Add(int vertexIndex)
        {
            writer.Write(vertexIndex);

        }
        public void EndWriting()
        {
            writer.Close();
        }
        public void SetupReading(string name = "")
        {
            if (name != "")
            {
                filepath = Application.persistentDataPath + "/" + name + ".dat";
            }
            fs = File.OpenRead(filepath);
            bReader = new BinaryReader(fs);
        }

        public int numberOfVertices()
        {
            return (int)fs.Length / 4;
        }
        public int ReadNext()
        {
            
            int output = bReader.ReadInt32();

            return output;
        }
        
        public int[] ReadAllItems()
        {
            if (fs == null)
            {
                throw new System.InvalidOperationException("Reader not initialized. Call SetupReading() first.");
            }

            long numInts = fs.Length / 4;
            int[] allInts = new int[numInts];

            // Reset stream position
            fs.Position = 0;

            byte[] allBytes = new byte[numInts * 4];
            int bytesRead = fs.Read(allBytes, 0, allBytes.Length);

            if (bytesRead != allBytes.Length)
            {
                throw new IOException("Could not read the entire file.");
            }

            for (int i = 0; i < numInts; i++)
            {
                allInts[i] = System.BitConverter.ToInt32(allBytes, i * 4);
            }

            return allInts;
        }
        
        public void EndReading()
        {
            bReader.Close();
            fs.Close();

        }
        public void RemoveData()
        {
            File.Delete(filepath);
        }
    }
}

