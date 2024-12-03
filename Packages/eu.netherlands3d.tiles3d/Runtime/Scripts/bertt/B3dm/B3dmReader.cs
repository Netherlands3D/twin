using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
namespace Netherlands3D.Tiles3D
{
    public static class B3dmReader
    {
        public static B3dm ReadB3dm(BinaryReader reader)
        {
            var b3dmHeader = new B3dmHeader(reader);
            var featureTableJson = Encoding.UTF8.GetString(reader.ReadBytes(b3dmHeader.FeatureTableJsonByteLength));
            var featureTableBytes = reader.ReadBytes(b3dmHeader.FeatureTableBinaryByteLength);

            string batchTableJson = null;
            if (b3dmHeader.BatchTableJsonByteLength != 0)
            {
                batchTableJson = Encoding.UTF8.GetString(reader.ReadBytes(b3dmHeader.BatchTableJsonByteLength));
            }
            byte[] batchTableBytes = null;
            if (b3dmHeader.BatchTableBinaryByteLength != 0)
            {
                batchTableBytes = reader.ReadBytes(b3dmHeader.BatchTableBinaryByteLength);
            }


            var glbLength = b3dmHeader.fileLength - b3dmHeader.Length;

            var glbBuffer = reader.ReadBytes(glbLength);

            // remove the trailing glb padding characters if any

            //int stride = 8;
            byte paddingbyte = Encoding.UTF8.GetBytes(" ")[0];
            List<byte> bytes = new List<byte>();
            bytes.Capacity = glbBuffer.Length;
            for (int i = 0; i < glbLength; i++)
            {

                bytes.Add(glbBuffer[i]);

            }
            //readGltfByteSize
            glbLength = bytes[11] * 256;
            glbLength = (glbLength + bytes[10]) * 256;
            glbLength = (glbLength + bytes[9]) * 256;
            glbLength = glbLength + bytes[8];

            for (int i = bytes.Capacity - 1; i >= glbLength; i--)
            {
                bytes.RemoveAt(i);
            }

            glbBuffer = bytes.ToArray();

            var b3dm = new B3dm
            {
                B3dmHeader = b3dmHeader,
                GlbData = glbBuffer,
                FeatureTableJson = featureTableJson,
                FeatureTableBinary = featureTableBytes,
                BatchTableJson = batchTableJson,
                BatchTableBinary = batchTableBytes
            };
            return b3dm;
        }

        public static B3dm ReadB3dm(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                var b3dm = ReadB3dm(reader);
                return b3dm;
            }
        }
    }
}