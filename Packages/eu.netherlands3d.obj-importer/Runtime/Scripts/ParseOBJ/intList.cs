using System;
using UnityEngine;
using System.IO;
using System.Runtime.InteropServices;

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
        
        public void ReadAllItems(int[] arrayToFill)
        {
            if (fs == null)
            {
                throw new System.InvalidOperationException("Reader not initialized. Call SetupReading() first.");
            }

            long numVectors = numberOfVertices();
            // Reset reader position to start of file
            var vectorSpan = new Span<int>(arrayToFill, 0, (int)numVectors);

            // reader.Read directly into that byteSpan
            fs.Position = 0;
            fs.Read(MemoryMarshal.AsBytes(vectorSpan));
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

