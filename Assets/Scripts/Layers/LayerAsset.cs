using System;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects.ExtensionMethods;

namespace Netherlands3D.Twin.Layers
{
    /// <summary>
    /// A LayerAsset represents any asset, remote or embedded in the project, that is used in the
    /// properties associated with a Layer. Each asset is unique for a whole layer, this naming collisions should
    /// be taken into account.
    ///
    /// LayerAssets are used when saving the project to determine whether additional files should be embedded in the
    /// project file, the URI scheme of these assets is 'project'. Remote assets are also supported.
    /// </summary>
    public class LayerAsset
    {
        /// <summary>
        /// Remember the property data to which this asset belongs, so that we can include the identifier
        /// when persisting the asset to disk.
        /// </summary>
        private readonly LayerPropertyData layerPropertyData;

        public LayerPropertyData LayerPropertyData => layerPropertyData;

        /// <summary>
        /// Where can this asset be found, if the scheme is 'project' then we should
        /// fetch the asset from the project file's folder and save it with the project.
        /// </summary>
        private readonly Uri uri;

        public Uri Uri => uri;

        public bool IsStoredInProject => Uri.IsStoredInProject();
        public bool IsRemoteAsset => Uri.IsRemoteAsset();
        
        public LayerAsset(LayerPropertyData layerPropertyData, Uri uri)
        {
            this.layerPropertyData = layerPropertyData;
            this.uri = uri;
        }
    }
}