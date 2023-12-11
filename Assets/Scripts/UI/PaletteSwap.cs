using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class PaletteSwap : MonoBehaviour
    {
        [SerializeField] private ColorPalette palette;
        // [SerializeField] private Texture2D paletteTexture;
        [SerializeField] private Material material;        

        private static readonly int PaletteTex = Shader.PropertyToID("_PaletteTex");

        private void Awake()
        {
            UpdatePalette();
        }

        private void Start()
        {
            
        }

        private void OnValidate()
        {
            Awake();
        }

        public void UpdatePalette()
        {
            material.SetTexture(PaletteTex, GetTexture());
            // material.SetTexture(PaletteTex, paletteTexture);
        }

        private Texture2D GetTexture()
        {
            Texture2D newTexture = new Texture2D(palette.Colors.Count, 1, TextureFormat.RGBA32, false);
            print(palette.Colors.Count);
            for (int i = 0; i < palette.Colors.Count; i++)
            {
                newTexture.SetPixel(i, 0, palette.Colors[i].Color);
                var x = (float)i/(float)palette.Colors.Count;
                print(x + "\t" + palette.Colors[i].Name);
            }

            newTexture.filterMode = FilterMode.Point;
            newTexture.wrapMode = TextureWrapMode.Clamp;
            newTexture.Apply();

            return newTexture;
        }
    }
}