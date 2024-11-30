using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
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
        public static IEnumerator LoadContent(string url, Transform containerTransform, Tile tile, Action<bool> succesCallback, bool parseAssetMetaData = false, bool parseSubObjects = false, UnityEngine.Material overrideMaterial = null, bool bypassCertificateValidation = false, Dictionary<string, string> customHeaders = null)
        {

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
                succesCallback.Invoke(false);
                yield break;
            }
            #endregion
            byte[] contentBytes = webRequest.downloadHandler.data;

            #region get contentType
            // get contentType
            ContentType contentType = getContentTypeFromBinaryHeader(contentBytes);
            if (contentType==ContentType.undefined)
            {
                contentType = getContentTypeFromFileExtension(url);
            }
            if (contentType == ContentType.undefined)
            {
                succesCallback.Invoke(false);
                yield break;
            }
            #endregion region

            //handle data
            switch (contentType)
            {
                case ContentType.b3dm:
                    Debug.Log("loadin b3dm");
                    ImportB3dm.LoadB3dm(contentBytes, tile, containerTransform, succesCallback, url,parseAssetMetaData,parseSubObjects,overrideMaterial);
                    break;
                case ContentType.pnts:
                    ImportPnts.LoadPoints(contentBytes, tile, containerTransform, succesCallback, url, parseAssetMetaData, parseSubObjects, overrideMaterial);
                    break;
                case ContentType.i3dm:
                    break;
                case ContentType.cmpt:
                    break;
                case ContentType.glb:
                    break;
                case ContentType.gltf:
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
