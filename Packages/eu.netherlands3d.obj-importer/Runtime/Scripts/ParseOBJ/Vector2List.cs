using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Netherlands3D.ObjImporter.ParseOBJ
{
    public class Vector2List
    {
       BinaryWriter writer;
       FileStream reader;
        int baseindex;
        float[] vectorFloatArray = new float[2];
        byte[] vectorBinaryArray = new byte[8];
        string basepath="";
        byte[] readBytes = new byte[8 * 1024];
        private int vectorCount = 0;
        string filepath;

        public int Count()
        {
            if (reader is null)
            {
                return vectorCount;
            }
            else
            {

           
            return  (int)reader.Length / 8;
            }
        }

        public void SetupWriting(string name)
        {
           

            if (basepath=="")
            {
                basepath = Application.persistentDataPath;
            }
            filepath = System.IO.Path.Combine(basepath,name + ".dat");
            writer = new BinaryWriter(File.Open(filepath, FileMode.Create,FileAccess.Write,FileShare.None));



        }
       
        public void Add(float v1, float v2)
        {
            
            vectorFloatArray[0] = v1;
            vectorFloatArray[1] = v2;
            System.Buffer.BlockCopy(vectorFloatArray, 0, vectorBinaryArray, 0, 8);
            writer.Write(vectorBinaryArray);
            vectorCount += 1;
        }
        public void EndWriting()
        {
            
            writer.Close();
            writer = null;
        }
        public void SetupReading(string name = "")
        {

            if (name !="")
            {
                filepath = Application.persistentDataPath + "/" + name + ".dat";
            }
            reader = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.None, 8);
            //reader = File.OpenRead(filepath);
            baseindex = -1;

        }
        public Vector2 ReadItem(int index)
        {
            bool readNewBatch = false;
            if (baseindex == -1)
            {
                readNewBatch = true;
            }
            else if (index < baseindex)
            {
                readNewBatch = true;
            }
            else if (index >= baseindex + 1024)
            {
                readNewBatch = true;
            }

            if (readNewBatch)
            {
                reader.Position = index * 8;
                baseindex = index;
                int count = reader.Read(readBytes, 0, 1024 * 8);
                //System.Buffer.BlockCopy(readBytes, 0, BiglistVectorFloatArray, 0, 1024*12);
            }
            //reader.Position = index * 12;
            Vector2 ReturnItem = new Vector2();
            //reader.Read(readBytes, 0, 12);
            int startindex = index - baseindex;
            System.Buffer.BlockCopy(readBytes, startindex*8, vectorFloatArray, 0, 8);
            
            ReturnItem.x = vectorFloatArray[0];
            ReturnItem.y = vectorFloatArray[1];
           // ReturnItem.z = vectorFloatArray[2];


            return ReturnItem;
        }
        
        public Vector2[] ReadAllItems()
        {
            if (reader == null)
            {
                throw new System.InvalidOperationException("Reader not initialized. Call SetupReading() first.");
            }

            long numVectors = Count();
            Vector2[] allVectors = new Vector2[numVectors];

            reader.Position = 0;

            byte[] allBytes = new byte[numVectors * 8];
            int bytesRead = reader.Read(allBytes, 0, allBytes.Length);

            if (bytesRead != allBytes.Length)
            {
                throw new System.IO.IOException("Could not read the entire file.");
            }

            float[] floatArray = new float[2];

            for (int i = 0; i < numVectors; i++)
            {
                System.Buffer.BlockCopy(allBytes, i * 8, floatArray, 0, 8);
                allVectors[i] = new Vector2(floatArray[0], floatArray[1]);
            }

            return allVectors;
        }
        
        public void EndReading()
        {
            reader.Close();
            reader = null;
            
        }
        public void RemoveData()
        {
            vectorCount = 0;
            File.Delete(filepath);
        }
    }
}
