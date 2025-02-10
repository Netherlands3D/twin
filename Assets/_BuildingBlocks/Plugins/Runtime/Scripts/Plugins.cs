using System.Collections.Generic;
using RSG;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Netherlands3D.Plugins
{
    [CreateAssetMenu(menuName = "Netherlands3D/Plugins/Create Plugins Registry", fileName = "Plugins", order = 0)]
    public class Plugins : ScriptableObject
    {
        private readonly List<PluginManifest> plugins = new();

        public IPromise<PluginManifest> RegisterPlugin(AssetReferenceT<PluginManifest> plugin)
        {
            var promise = new Promise<PluginManifest>();
            Addressables.LoadAssetAsync<PluginManifest>(plugin).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    this.RegisterPlugin(handle.Result);
                    promise.Resolve(handle.Result);
                }
                else
                {
                    Debug.LogError($"Failed to load plugin manifest {handle.OperationException.Message}");
                    promise.Reject(handle.OperationException);
                }
            };
            
            return promise;
        }

        public IPromise<PluginManifest> RegisterPlugin(PluginManifest plugin)
        {
            if (plugins.Contains(plugin))
            {
                return Promise<PluginManifest>.Resolved(plugin);
            }

            plugin.Register(plugins);
            Debug.Log($"Registered plugin '{plugin.displayName}' with Plugins");

            return Promise<PluginManifest>.Resolved(plugin);
        }

        public PluginManifest FindById(string id)
        {
            return plugins.Find(plugin => plugin.id == id);
        }

        public IEnumerable<PluginManifest> GetAllPlugins()
        {
            return plugins;
        }
    }
}