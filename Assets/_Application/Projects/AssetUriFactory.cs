using System;
using System.IO;
using UnityEngine;

namespace Netherlands3D.Twin.Projects
{
    public static class AssetUriFactory
    {
        public static Uri CreateProjectAssetUri(string path)
        {
            if (path.StartsWith("project:///"))
                return new Uri(path);
            
            if (path.StartsWith(Application.persistentDataPath))
                path = Path.GetRelativePath(Application.persistentDataPath, path);
            
            return new Uri("project:///" + path);
        }

        public static Uri CreateRemoteAssetUri(string url)
        {
            if (!url.StartsWith("http") && !url.StartsWith("https")) return null;
            
            return new Uri(url);
        }

        public static string GetLocalPath(Uri projectAssetUri)
        {
            if (projectAssetUri.Scheme =="project")
            {
                string localPath = projectAssetUri.LocalPath.TrimStart('/', '\\');
                return Path.Combine(Application.persistentDataPath, localPath);
            }

            return null;
        }
    }
}