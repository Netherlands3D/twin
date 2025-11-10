using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Netherlands3D.Tiles3D.Editor
{
    [InitializeOnLoad]
    internal sealed class TestDataResourceSync : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string SourcePath = "Packages/eu.netherlands3d.tiles3d/TestData";
        private const string TargetPath = "Packages/eu.netherlands3d.tiles3d/Runtime/Resources/TestData";

        private static bool removedForReleaseBuild;

        static TestDataResourceSync()
        {
            EditorApplication.delayCall += EnsureEditorHasTestData;
        }

        private static void EnsureEditorHasTestData()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            MirrorToResources(logPrefix: "[Tiles3D] Editor auto-sync");
        }

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            bool isDevelopmentBuild = (report.summary.options & BuildOptions.Development) != 0;

            if (isDevelopmentBuild)
            {
                MirrorToResources(logPrefix: "[Tiles3D] Dev build sync");
                removedForReleaseBuild = false;
            }
            else
            {
                removedForReleaseBuild = RemoveFromResources(logPrefix: "[Tiles3D] Release build cleanup");
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (removedForReleaseBuild)
            {
                MirrorToResources(logPrefix: "[Tiles3D] Restore after release build");
                removedForReleaseBuild = false;
            }
        }

        private static void MirrorToResources(string logPrefix)
        {
            if (!Directory.Exists(SourcePath))
            {
                Debug.LogWarning($"{logPrefix}: bronmap '{SourcePath}' ontbreekt; geen testdata beschikbaar.");
                return;
            }

            RemoveFromResources(logPrefix: null);

            FileUtil.CopyFileOrDirectory(SourcePath, TargetPath);
            AssetDatabase.Refresh();

            if (!string.IsNullOrEmpty(logPrefix))
            {
                Debug.Log($"{logPrefix}: TestData beschikbaar gemaakt in Resources.");
            }
        }

        private static bool RemoveFromResources(string logPrefix)
        {
            bool removedSomething = false;

            if (Directory.Exists(TargetPath))
            {
                FileUtil.DeleteFileOrDirectory(TargetPath);
                removedSomething = true;
            }

            string metaPath = TargetPath + ".meta";
            if (File.Exists(metaPath))
            {
                FileUtil.DeleteFileOrDirectory(metaPath);
                removedSomething = true;
            }

            if (removedSomething)
            {
                AssetDatabase.Refresh();

                if (!string.IsNullOrEmpty(logPrefix))
                {
                    Debug.Log($"{logPrefix}: TestData verwijderd uit Resources.");
                }
            }

            return removedSomething;
        }
    }
}
