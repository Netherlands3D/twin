using System;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Services;
using UnityEngine;

namespace Netherlands3D.Functionalities.Wfs
{
    public class WfsLayerBuilder : BaseLayerBuilder
    {
        public WfsLayerBuilder(
            Uri featureUrl, 
            string title, 
            FolderLayer parent
        ) {
            Debug.Log($"Adding WFS layer with url '{featureUrl}'");

            var randomLayerColor = LayerColor.Random();
            
            var styling = new Symbolizer();
            styling.SetFillColor(randomLayerColor);
            styling.SetStrokeColor(randomLayerColor);

            OfType("b1bd3a7a50cb3bd4bb3236aadf5c32b6");
            NamedAs(title);
            ChildOf(parent);
            WithColor(randomLayerColor);
            SetDefaultStyling(styling);
            AddProperty(new LayerURLPropertyData
            {
                Data = AssetUriFactory.CreateRemoteAssetUri(featureUrl.ToString())
            });
        }    
    }
}