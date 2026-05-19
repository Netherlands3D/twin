using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.LayerStyles;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Color")]
    public class ColorPropertyData : StylingPropertyData
    {
        [JsonConstructor]
        public ColorPropertyData()
        {
        }

        //This could be changed to a data member if we want to save the last used coloring type in the project file (eg. stroke or fill)
        [JsonIgnore] private string colorType = Symbolizer.FillColorProperty; //default

        [JsonIgnore]
        public string ColorType
        {
            get => colorType;
            set
            {
                colorType = value;
                ColorTypeChanged.Invoke(value);
            }
        }

        public void SetDefaultSymbolizerColor(Color? color)
        {
            var symbolizer = AnyFeature.Symbolizer;
            symbolizer.SetColor(ColorType, color);
            OnStylingChanged.Invoke();
        }

        public Color? GetDefaultSymbolizerColor()
        {
            var symbolizer = AnyFeature.Symbolizer;
            return symbolizer.GetColor(ColorType);
        }

        public override List<string> GetUsedColorTypes()
        {
            var list = DefaultSymbolizer.GetUsedColorProperties(); 
            
            if (!list.Contains(ColorType))
                list.Add(ColorType); //todo: this is the currently activated Color property, not a list of all supported color properties. We currently do not have a method of determining which color types are supported.
    
            return list; 
        }
    }
}