using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Twin
{
    [Serializable]
    public struct NamedColor
    {
        public string Name;
        public Color Color;
    }
    
    [CreateAssetMenu(menuName = "Netherlands3D/ColorPalette", fileName = "ColorPalette", order = 0)]
    public class ColorPalette : ScriptableObject
    {
        public List<NamedColor> Colors = new();

        public Color GetColorByName(string name)
        {
            return Colors.FirstOrDefault(nc => nc.Name == name).Color;
        }
    }
}
