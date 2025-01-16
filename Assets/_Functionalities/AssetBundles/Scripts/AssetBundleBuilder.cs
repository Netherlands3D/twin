#if UNITY_EDITOR
using UnityEditor;

namespace Netherlands3D.Functionalities.AssetBundles
{
    public class AssetBundleBuilder
    {
        [MenuItem("Netherlands3D/Build AssetBundles")]
        static void BuildAllAssetBundles()
        {
            BuildPipeline.BuildAssetBundles("Assets/StreamingAssets", BuildAssetBundleOptions.None, BuildTarget.WebGL);
        }
    }
}
#endif