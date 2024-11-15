using System;

namespace Netherlands3D.Twin.Projects
{
    public static class AssetUriFactory
    {
        public static Uri CreateProjectAssetUri(string pathInProject)
        {
            return new Uri("project:///" + pathInProject);
        }

        public static Uri CreateRemoteAssetUri(string url)
        {
            if (!url.StartsWith("http") && !url.StartsWith("https")) return null;
            
            return new Uri(url);
        }

        public static Uri CreateRemoteAssetUri(Uri url)
        {
            return url.Scheme is "http" or "https" ? url : null;
        }
    }
}