using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor.PackageManager;

public class PackageSwitcher : EditorWindow
{
    private Dictionary<string, bool> packageToggles = new Dictionary<string, bool>();
    private Dictionary<string, string> localPackagePaths = new Dictionary<string, string>();
    private Dictionary<string, string> remoteVersions = new Dictionary<string, string>();

    private string packageNamePrefix = "eu.netherlands3d";
    private static string localPackagesFolderPrefix = "../../Packages";
    private static string originalVersionsFilePath = "../Packages/original_versions.json";
    private static string localPackagesFolderFullPath = "";
    
    [MenuItem("Tools/Package Switcher")]
    public static void ShowWindow()
    {
        var window = GetWindow<PackageSwitcher>("Package Switcher");
        originalVersionsFilePath = Path.GetFullPath(Path.Combine(Application.dataPath, originalVersionsFilePath));
        localPackagesFolderFullPath = Path.GetFullPath(Path.Combine(Application.dataPath, localPackagesFolderPrefix));

        window.LoadOriginalVersions();
        window.LoadPackages(); // Load packages when the window is opened
        window.AutoPopulateLocalPackagePaths();
    }

    private void OnGUI()
    {
        GUILayout.Label("Package Switcher", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("local packages folder", EditorStyles.boldLabel, GUILayout.Width(200));
        localPackagesFolderFullPath = EditorGUILayout.TextField(localPackagesFolderFullPath);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("show only packages with prefix", EditorStyles.boldLabel, GUILayout.Width(200));
        packageNamePrefix = EditorGUILayout.TextField(packageNamePrefix);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("original versions path", EditorStyles.boldLabel, GUILayout.Width(200));
        originalVersionsFilePath = EditorGUILayout.TextField(originalVersionsFilePath);
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Label("Use Local", EditorStyles.boldLabel);

        // Create a list of package names to avoid modifying the dictionary while iterating
        var packageNames = new List<string>(packageToggles.Keys);

        foreach (var packageName in packageNames)
        {
            EditorGUILayout.BeginHorizontal();

            // Toggle for local/remote
            bool newToggleValue = EditorGUILayout.ToggleLeft(packageName, packageToggles[packageName], GUILayout.Width(300));

            // Text field for local path
            string newLocalPath = EditorGUILayout.TextField(localPackagePaths[packageName]);

            // Display the remote version
            GUILayout.Label(remoteVersions[packageName], GUILayout.Width(100));

            EditorGUILayout.EndHorizontal();

            // Update the dictionaries after the loop
            if (newToggleValue != packageToggles[packageName])
            {
                packageToggles[packageName] = newToggleValue;
            }
            if (newLocalPath != localPackagePaths[packageName])
            {
                localPackagePaths[packageName] = newLocalPath;
            }
        }

        if (GUILayout.Button("Apply Changes"))
        {
            ApplyChanges();
        }
    }

    private void LoadPackages()
    {
        string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
        if (File.Exists(manifestPath))
        {
            string json = File.ReadAllText(manifestPath);
            JObject manifest = JObject.Parse(json);

            var dependencies = manifest["dependencies"] as JObject;
            if (dependencies != null)
            {
                foreach (var dependency in dependencies)
                {
                    string packageName = dependency.Key;
                    if (packageNamePrefix != "" && !packageName.StartsWith(packageNamePrefix))
                    {
                        continue;
                    }
                    
                    string packageVersion = dependency.Value.ToString();

                    // Assume local paths start with "file:"
                    bool isLocal = packageVersion.StartsWith("file:");
                    packageToggles[packageName] = isLocal;
                    localPackagePaths[packageName] = isLocal ? packageVersion.Substring(5) : "";
                    if (!isLocal && !remoteVersions.ContainsKey(packageName))
                    {
                        remoteVersions[packageName] = packageVersion;
                    }
                }
            }
            SaveOriginalVersions();
        }
        else
        {
            Debug.LogError("manifest.json not found!");
        }
    }

    private void AutoPopulateLocalPackagePaths()
    {
        if (Directory.Exists(localPackagesFolderFullPath))
        {
            foreach (var packageDir in Directory.GetDirectories(localPackagesFolderFullPath))
            {
                string packageJsonPath = Path.Combine(packageDir, "package.json");
                if (File.Exists(packageJsonPath))
                {
                    string json = File.ReadAllText(packageJsonPath);
                    JObject packageJson = JObject.Parse(json);

                    string packageName = packageJson["name"]?.ToString();
                    if (!string.IsNullOrEmpty(packageName) && packageToggles.ContainsKey(packageName))
                    {
                        localPackagePaths[packageName] = Path.GetFullPath(packageDir);
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Packages folder not found!");
        }
    }
    
    private void ApplyChanges()
    {
        string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
        if (File.Exists(manifestPath))
        {
            string json = File.ReadAllText(manifestPath);
            JObject manifest = JObject.Parse(json);

            var dependencies = manifest["dependencies"] as JObject;
            if (dependencies != null)
            {
                // Create a list of package names to avoid modifying the collection while iterating
                var packageNames = new List<string>(packageToggles.Keys);

                foreach (var packageName in packageNames)
                {
                    if (packageToggles[packageName])
                    {
                        // Switch to local version
                        dependencies[packageName] = $"file:{localPackagePaths[packageName]}";
                    }
                    else
                    {
                        // Switch to remote version
                        dependencies[packageName] = remoteVersions[packageName];
                    }
                }

                File.WriteAllText(manifestPath, manifest.ToString(Newtonsoft.Json.Formatting.Indented));
                Client.Resolve();

                Close(); //close the window, because it somehow becomes empty
                EditorApplication.delayCall += () =>
                {
                    ShowWindow();
                };
                
                Debug.Log("Package toggles applied.");
            }
        }
        else
        {
            Debug.LogError("manifest.json not found!");
        }
    }
    
    private void SaveOriginalVersions()
    {
        string json = JsonConvert.SerializeObject(remoteVersions, Formatting.Indented);
        File.WriteAllText(originalVersionsFilePath, json);
    }

    private void LoadOriginalVersions()
    {
        if (File.Exists(originalVersionsFilePath))
        {
            string json = File.ReadAllText(originalVersionsFilePath);
            remoteVersions = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }
    }
}
