using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;

using System.Threading.Tasks;


#if UNITY_EDITOR
using System.IO.Compression;
#endif
namespace Netherlands3D.Tiles3D
{
    public class TIleContentLoader
    {


        enum ContentType
        {
            undefined,
            b3dm,
            pnts,
            i3dm,
            cmpt,
            glb,
            gltf,
            subtree,
            tileset
        }

        private static CustomCertificateValidation customCertificateHandler = new CustomCertificateValidation();
        /// <summary>
        /// Helps bypassing expired certificate warnings.
        /// Use with caution, and only with servers you trust.
        /// </summary>
        public class CustomCertificateValidation : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                return true;
            }
        }
        public static IEnumerator DownloadContent(string url, Transform containerTransform, Tile tile, Action<byte[],string> Callback, bool parseAssetMetaData = false, bool parseSubObjects = false, UnityEngine.Material overrideMaterial = null, bool bypassCertificateValidation = false, Dictionary<string, string> customHeaders = null)
        {
            Debug.Log("starting download");
            #region download data
            var webRequest = UnityWebRequest.Get(url);
            if (customHeaders != null)
            {
                foreach (var header in customHeaders)
                    webRequest.SetRequestHeader(header.Key, header.Value);
            }

            if (bypassCertificateValidation)
                webRequest.certificateHandler = customCertificateHandler; //Not safe; but solves breaking curl error

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(url + " -> " + webRequest.error);
                Callback.Invoke(null,url);
                yield break;
            }
            #endregion
            byte[] contentBytes = webRequest.downloadHandler.data;
            Debug.Log("downloaded data");
            Callback.Invoke(contentBytes, url);
            
            
        }

        public static async Task LoadContent(byte[] contentBytes,string sourceUri, Transform containerTransform, Tile tile, Action<bool> succesCallback, bool parseAssetMetaData = false, bool parseSubObjects = false, UnityEngine.Material overrideMaterial = null, bool bypassCertificateValidation = false, Dictionary<string, string> customHeaders = null)
        {

            #region get contentType
            // get contentType
            ContentType contentType = getContentTypeFromBinaryHeader(contentBytes);

            if (contentType == ContentType.undefined)
            {
                contentType = ContentType.gltf;
            }
            #endregion region

            ImportGlb importGlb;

            //handle data
            switch (contentType)
            {
                case ContentType.b3dm:
                     await ImportB3dm.LoadB3dm(contentBytes, tile, containerTransform, succesCallback, "", parseAssetMetaData, parseSubObjects, overrideMaterial);
                    break;
                case ContentType.pnts:
                    Debug.LogError(".pnts is not supported");
                    succesCallback.Invoke(false);
                    //Debug.Log("loading pnts");
                    //await ImportPnts.LoadPoints(contentBytes, tile, containerTransform, succesCallback, "", parseAssetMetaData, parseSubObjects, overrideMaterial);
                    break;
                case ContentType.i3dm:
                    Debug.LogError(".i3dm is not supported");
                    succesCallback.Invoke(false);
                    //Debug.Log("loading i3dm");
                    //await ImportI3dm.Load(contentBytes, tile, containerTransform, succesCallback, "", parseAssetMetaData, parseSubObjects, overrideMaterial);
                    break;
                case ContentType.cmpt:
                    Debug.LogError(".cmpt is not supported");
                    succesCallback.Invoke(false);
                    //Debug.Log("loading cmpt");
                    //await ImportComposite.Load(contentBytes, tile, containerTransform, succesCallback, "", parseAssetMetaData, parseSubObjects, overrideMaterial);
                    break;
                case ContentType.glb:
                    importGlb = new ImportGlb();
                    await importGlb.Load(contentBytes, tile, containerTransform, succesCallback, "", parseAssetMetaData, parseSubObjects, overrideMaterial);

                    break;
                case ContentType.gltf:
                    ImportGltf importGltf = new ImportGltf();
                    await importGltf.Load(contentBytes, tile, containerTransform, succesCallback,sourceUri, parseAssetMetaData, parseSubObjects, overrideMaterial);

                    break;
                case ContentType.subtree:
                    break;
                case ContentType.tileset:
                    break;
                default:
                    break;
            }
        }

        static ContentType getContentTypeFromBinaryHeader(byte[] content)
        {
            //readMagic bytes
            string magic = Encoding.UTF8.GetString(content, 0, 4);
            Debug.Log("magic: " + magic);
            switch (magic)
            {
                case "b3dm":
                    return ContentType.b3dm;
                case "pnts":
                    return ContentType.pnts;
                case "i3dm":
                    return ContentType.i3dm;
                case "cmpt":
                    return ContentType.cmpt;
                case "glTF":
                    return ContentType.glb;
                case "subt":
                    return ContentType.subtree;
                default:
                    return ContentType.undefined;
            }
        }

        static ContentType getContentTypeFromFileExtension(string filename)
        {
            string filestring = filename;
            if (filestring.Contains("?"))
            {
                filestring = filestring.Split("?")[0];
            }
            if (filestring.Contains(".")==false)
            {
                return ContentType.undefined;

            }
            int pointposition = filestring.LastIndexOf(".");
            string extension = filestring.Substring(pointposition);
            switch (extension)
            {
                case "gltf":
                    return ContentType.gltf;
                case "json":
                    return ContentType.tileset;
                default:
                    return ContentType.undefined;
            }
        }
    }

}
