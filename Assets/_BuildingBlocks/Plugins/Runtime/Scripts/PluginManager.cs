using System.Collections;
using System.Collections.Generic;
using KindMen.Uxios;
using Netherlands3D.Twin.Projects;
using RSG;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;

namespace Netherlands3D.Plugins
{
    /// <summary>
    /// Preload any locally defined Plugins, and allow for adding plugins in runtime.
    ///
    /// It is recommended to add this manager to a loading scene to pre-load local plugins before the rest of the
    /// application is loaded, and it is possible to include it in the main scene to additively load more plugins on
    /// runtime.
    ///
    /// The Plugins Data Container will graciously handle it when you attempt to load the same plugin multiple times,
    /// so no additional checking is necessary. 
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class PluginManager : MonoBehaviour
    {
        [SerializeField] private Plugins plugins;
        [SerializeField] private PrefabLibrary prefabLibrary;
        [SerializeField] private List<AssetReferenceT<PluginManifest>> preloadedLocalPlugins = new();
        public UnityEvent completedPreloadingLocalPlugins = new();
        
        private IEnumerator Start()
        {
            foreach (var plugin in preloadedLocalPlugins)
            {
                yield return LoadPlugin(plugin);
            }
            
            completedPreloadingLocalPlugins.Invoke();
        }

        public IEnumerator LoadPlugin(AssetReferenceT<PluginManifest> plugin)
        {
            // Instead of using promises, we 'convert' the promise to a yield instruction to play nice with
            // the fact that start is an IEnumerator and to make it easier to integrate into unity.
            var futurePlugin = plugins.RegisterPlugin(plugin) as Promise<PluginManifest>;
            futurePlugin.Then(RegisterWithPrefabLibrary);
            yield return Uxios.WaitForRequest(futurePlugin);
        }

        public IEnumerator LoadPlugin(PluginManifest plugin)
        {
            // Instead of using promises, we 'convert' the promise to a yield instruction to play nice with
            // the fact that start is an IEnumerator and to make it easier to integrate into unity.
            var futurePlugin = plugins.RegisterPlugin(plugin) as Promise<PluginManifest>;
            futurePlugin.Then(RegisterWithPrefabLibrary);
            yield return Uxios.WaitForRequest(futurePlugin);
        }

        private void RegisterWithPrefabLibrary(PluginManifest pluginManifest)
        {
            foreach (var layerReference in pluginManifest.Layers)
            {
                // prefabLibrary
            }
        }
    }
}