using System;

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
    }
}