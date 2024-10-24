
#if UNITY_EDITOR 
using UnityEditor;
using UnityEngine;

public class AssetBundleBuilder
{
    [MenuItem("Assets/Build AssetBundles")]
    static void BuildAllAssetBundles()
    {
        BuildPipeline.BuildAssetBundles("Assets/StreamingAssets", BuildAssetBundleOptions.None, BuildTarget.WebGL);
    }
}
#endif