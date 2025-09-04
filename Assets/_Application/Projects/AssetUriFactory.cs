using System;
using System.IO;
using Netherlands3D.DataTypeAdapters;
using UnityEngine;

namespace Netherlands3D.Twin.Projects
{
    public static class AssetUriFactory
    {
        public static Uri ConvertLocalFileToAssetUri(LocalFile localFile)
        {
            var localPath = localFile.LocalFilePath;
            if (IsRemoteUri(localFile.SourceUrl))
            {
                return CreateRemoteAssetUri(localFile.SourceUrl);
            }

            return CreateProjectAssetUri(localPath);
        }

        public static Uri CreateProjectAssetUri(string path)
        {
            if (IsProjectUri(path)) return new Uri(path);

            if (path.StartsWith(Application.persistentDataPath))
            {
                path = Path.GetRelativePath(Application.persistentDataPath, path);
            }
            
            return new Uri("project:///" + path);
        }

        public static Uri CreateRemoteAssetUri(string url)
        {
            if (!IsRemoteUri(url)) return null;
            
            return new Uri(url);
        }

        public static string GetLocalPath(Uri projectAssetUri)
        {
            if (!IsProjectUri(projectAssetUri)) return null;

            string localPath = projectAssetUri.LocalPath.TrimStart('/', '\\');
            return Path.Combine(Application.persistentDataPath, localPath);
        }

        private static bool IsRemoteUri(string url)
        {
            return url.StartsWith("http://") && url.StartsWith("https://");
        }

        private static bool IsProjectUri(string url)
        {
            return url.StartsWith("project://");
        }

        private static bool IsProjectUri(Uri uri)
        {
            return uri.Scheme == "project";
        }
    }
}