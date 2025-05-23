﻿using UnityEngine;
using UnityEngine.Serialization;

namespace Netherlands3D.Twin.Quality
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/EnvironmentProfile", order = 0)]
    public class EnvironmentProfile : ScriptableObject
    {
        [FormerlySerializedAs("enviromentName")] public string environmentName = "Clear";

        public Texture2D skyIcon;

        public Color fogColorDay = Color.white;
        public Color fogColorNight = Color.black;

        //3 Color gradients
        public Color[] skyColorsDay = new Color[3];
        public Color[] skyColorsNight = new Color[3];

        [Header("Textured sky settings")]
        public bool isTexturedSky = true;
        public string texturePath = "skybox_grey.png";
        public Texture2D loadedTexture = null;

        public float exposureDay = 1.0f;
        public float exposureNight = 0.1f;

        public Color skyTintColorDay = Color.gray;
        public Color skyTintColorNight = Color.blue;

        public Texture2D sunTexture; 
        public Texture2D haloTexture; 
        public Color sunTextureTintColor = Color.yellow;
        public Color sunHaloTextureTintColor = Color.yellow;
    }
}
