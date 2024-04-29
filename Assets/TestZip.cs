using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using SharpZipLib.Unity.Helpers;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class TestZip : MonoBehaviour
    {
        ZipOutputStream zipOutputStream;
        void Start()
        {
            //Open our zip stream, and keep it open
            zipOutputStream = new ZipOutputStream(File.Create(Application.persistentDataPath + "/test.zip"));
            zipOutputStream.SetLevel(9); // 0 - store only to 9 - means best compression
        }

        // Append a file to zip
        public void AddFileToZip(string filePath)
        {
            byte[] buffer = new byte[4096];
            ZipEntry entry = new ZipEntry(filePath);
            zipOutputStream.PutNextEntry(entry);

            using (FileStream fs = File.OpenRead(filePath)) {
                StreamUtils.Copy(fs, zipOutputStream, buffer);
            }         
        }
    }
}
