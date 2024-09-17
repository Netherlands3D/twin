using System;
using System.IO;
using UnityEngine;

namespace Netherlands3D.Twin.Projects.ExtensionMethods
{
    public static class UriExtensions
    {
        public static bool IsStoredInProject(this Uri uri)
        {
            return uri.Scheme == "project";
        }

        public static bool IsRemoteAsset(this Uri uri)
        {
            return uri.Scheme is "http" or "https";
        }

        public static string ToProjectPath(this Uri uri)
        {
            if (uri.IsStoredInProject() == false)
            {
                return null;
            }

            return Path.Combine(Application.persistentDataPath, uri.LocalPath.TrimStart('/', '\\'));
        }
    }
}