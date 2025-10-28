using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Netherlands3D.Tiles3D
{
    public static class ImportComposite
    {

        public static async Task Load(byte[] data, Tile tile, Transform containerTransform, Action<bool> succesCallback, string sourcePath, bool parseAssetMetaData = false, bool parseSubObjects = false, UnityEngine.Material overrideMaterial = null)
        {

            Debug.Log("loading cmpt");
            var memoryStream = new System.IO.MemoryStream(data);
            BinaryReader reader = new BinaryReader(memoryStream);

            string Magic = Encoding.UTF8.GetString(reader.ReadBytes(4));
            int Version = (int)reader.ReadUInt32();
            int fileLength = (int)reader.ReadUInt32();
            int tilesLength = (int)reader.ReadUInt32();

            int tilestart = 16;
            for (int i = 0; i < tilesLength; i++)
            {
                memoryStream.Position = tilestart;
                reader = new BinaryReader(memoryStream);
                string tileMagic = Encoding.UTF8.GetString(reader.ReadBytes(4));
                int tileVersion = (int)reader.ReadUInt32();
                int blobLength = (int)reader.ReadUInt32();
                byte[] tiledata = new byte[blobLength];
                for (int j = 0; j < blobLength; j++)
                {
                    tiledata[j] = data[tilestart + j]; ;
                }
                if (tileMagic == "b3dm")
                {
                    Debug.Log("composit is loading b3dm");
                    await ImportB3dm.LoadB3dm(tiledata, tile, containerTransform, succesCallback, sourcePath, parseAssetMetaData, parseSubObjects, overrideMaterial);
                    Debug.Log("composit finished loading b3dm");
                }
                if (tileMagic == "i3dm")
                {
                    //Debug.Log("composit is loading i3dm");
                    //await ImportI3dm.Load(tiledata, tile, containerTransform, succesCallback, sourcePath, parseAssetMetaData, parseSubObjects, overrideMaterial);
                    //Debug.Log("composit finished loading i3dm");
                }

                tilestart += blobLength;
            }
            
        }

    }
}
