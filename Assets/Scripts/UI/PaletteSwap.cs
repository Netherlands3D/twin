using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;

    public class PaletteSwap : MonoBehaviour
    {
        private Image image;
        [SerializeField] private ColorPalette palette;

        private static readonly int PaletteTex = Shader.PropertyToID("_PaletteTex");

        private void Awake()
        {
            image = GetComponent<Image>();
            UpdatePalette();
        }

        private void OnValidate()
        {
            Awake();
        }

        public void UpdatePalette()
        {
            image.material.SetTexture(PaletteTex, GetTexture());
        }

        private Texture2D GetTexture()
        {
            Texture2D newTexture = new Texture2D(palette.Colors.Count, 1, TextureFormat.RGBA32, false);
            for (int i = 0; i < palette.Colors.Count; i++)
            {
                newTexture.SetPixel(i, 0, palette.Colors[i].Color);
            }

            newTexture.filterMode = FilterMode.Point;
            newTexture.wrapMode = TextureWrapMode.Clamp;
            newTexture.Apply();

            return newTexture;
        }
    }
}