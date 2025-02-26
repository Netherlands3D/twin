using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Plugins
{
    /// <summary>
    /// The Manifest file for a Plugin.
    ///
    /// Similar to a package manager manifest file, such as the `package.json` in NPM/UPM, or the Android Manifest file;
    /// we need something that can be retrieved without downloading the other resources in an Addressable. This manifest
    /// file can provide metadata -among which the plugin name, description and which layers should be exposed in what
    /// group of the object library-.
    ///
    /// By adding a label "manifest" to this asset in the addressable group, and ensuring that the `Bundle Mode` of an
    /// Addressable Group is set to `Pack together by Label`, Unity will pack the manifest file in a separate asset
    /// bundle and the asset can be loaded without loading the rest of the assets bundle as the others are in a separate
    /// bundle. See https://docs.unity3d.com/Packages/com.unity.addressables@2.0/manual/PackingGroupsAsBundles.html for
    /// more information about this functionality.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Never use references to Scripts or Assets outside of the Plugin package - and even within this:
    /// minimize it. This ScriptableObject is meant as a super-lightweight manifest file that can be imported without
    /// other dependencies. Especially GameObject -or prefab- references are ILLEGAL in this file as it will explode
    /// the asset bundle size due to extra dependencies. 
    /// </remarks>
    /// <see href="https://en.wikipedia.org/wiki/Manifest_file"/>
    /// <see href="https://docs.unity3d.com/Manual/upm-manifestPkg.html"/>
    /// <see href="https://yarnpkg.com/configuration/manifest"/>
    /// <see href="https://developer.android.com/guide/topics/manifest/manifest-intro"/>
    /// <see href="https://docs.unity3d.com/Packages/com.unity.addressables@2.0/manual/PackingGroupsAsBundles.html"/>
    [CreateAssetMenu(menuName = "Create Plugin Manifest", fileName = "PluginManifest", order = 0)]
    public class PluginManifest : ScriptableObject
    {
        [Tooltip("Should be in the package format and generally matches the package name: 'eu.netherlands3d.plugins.[x]'")]
        public string id;
        
        [Tooltip("The name that will be displayed on screen, or in debug tooling.")]
        public string displayName;
        
        [Tooltip("A list of layers that will be exposed, and registered with the Object Library")]
        [SerializeField] private List<PluginManifestLayerReference> layers = new();

        [Tooltip("Will be called when this plugin is registered, can be used to trigger initialization")]
        public UnityEvent<PluginManifest> registered = new();

        public List<PluginManifestLayerReference> Layers => layers;

        public void Register(List<PluginManifest> plugins)
        {
            plugins.Add(this);
            registered.Invoke(this);
        }
    }
}
