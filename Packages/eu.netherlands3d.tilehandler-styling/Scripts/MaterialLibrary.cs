using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.TileHandler
{
    public class MaterialLibrary : MonoBehaviour
    {
        [SerializeField]
        private Material[] materialLibrary;

        [SerializeField]
        private bool runtimeCompressLoadedTextures = false;

        [SerializeField]
        private float materialColorMatchingThreshold = 0.01f;

        public static MaterialLibrary Instance;

        [SerializeField]
        private string htmlTexturesFolderDesktop = "TexturesDesktop";

        [SerializeField]
        private string htmlTexturesFolderMobile = "TexturesMobile";

        [SerializeField]
        private string textureFileExtention = ".png";

        private Dictionary<string, Texture2D> loadedTextures;

        private const string materialBaseMap = "_BaseMap";

        private void Awake()
		{
            Instance = this;
            loadedTextures = new Dictionary<string, Texture2D>();
        }

		private void Start()
		{
            StartCoroutine(LoadTextures());
        }

		private IEnumerator LoadTextures()
        {
            var texturesFolder = (false/*ApplicationSettings.Instance.IsMobileDevice*/) ? htmlTexturesFolderMobile : htmlTexturesFolderDesktop;
            var relativeDirectory = Application.dataPath;
#if UNITY_EDITOR
            relativeDirectory = $"file:///{relativeDirectory}/WebGLTemplates/Netherlands3D";
#endif

            foreach (Material material in materialLibrary)
            {
                //If this material name contains an underscore, use the last part as a texture path
                if (material.name.Contains("[texture="))
                {
                    var materialTextureFileName = material.name.Split('=')[1].Replace("]","");
                    if (loadedTextures.ContainsKey(materialTextureFileName))
                    {
                        material.SetTexture(materialBaseMap, loadedTextures[materialTextureFileName]);
                    }
                    else
                    {
                        var materialTextureUrl = $"{relativeDirectory}/{texturesFolder}/{materialTextureFileName}";
                        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(materialTextureUrl))
                        {
                            yield return uwr.SendWebRequest();
                            if (uwr.result != UnityWebRequest.Result.Success)
                            {
                                Debug.Log($"No texture found for material {materialTextureUrl}");
                            }
                            else
                            {
                                // Get downloaded asset bundle
                                var loadedTexture = (Texture2D)DownloadHandlerTexture.GetContent(uwr);

                                // Mipmapped texture
                                var mipTexture = new Texture2D(loadedTexture.width, loadedTexture.height, TextureFormat.ARGB32,true);

                                // Copy the pixels over to mipmap 0
                                mipTexture.SetPixels(loadedTexture.GetPixels());
                                mipTexture.Apply(); //Apply now generates our mipmap steps
                                if(runtimeCompressLoadedTextures) mipTexture.Compress(false);

                                Destroy(loadedTexture);

                                loadedTextures.Add(materialTextureFileName, mipTexture);
                                material.SetTexture(materialBaseMap, mipTexture);
                            }
                        }
                    }
                }
            }
        }

		/// <summary>
		/// Remaps materials to this object based on material name / substrings
		/// </summary>
		/// <param name="renderer">The GameObject containing the renderer with the materials list</param>
		public bool AutoRemap(GameObject gameObjectWithRenderer)
		{
			var renderer = gameObjectWithRenderer.GetComponent<MeshRenderer>();
			if (!renderer)
			{
				Debug.LogWarning("No meshrenderer found in this GameObject. Skipping auto remap.");
				return false;
			}

            var matchedMaterialNames = FoundMatch(renderer);
            if (matchedMaterialNames.Count > 0)
            {
	            return true;
            }
            return false;
		}
		
        private List<string> FoundMatch(MeshRenderer renderer)
        {
            var materialArray = renderer.sharedMaterials;
            List<string> materialNames = new List<string>();
            for (int i = 0; i < materialArray.Length; i++)
            {
                if (materialArray[i] != FindMaterialReplacement(materialArray[i]))
                    materialNames.Add(materialArray[i].name.Replace("(Clone)", "").Replace("(Instance)", ""));
            }
            return materialNames;
        }

        /// <summary>
        /// Finds a material from the library with a similar name
        /// </summary>
        /// <param name="comparisonMaterial">The material to find a library material for</param>
        /// <returns></returns>
        public Material FindMaterialReplacement(Material comparisonMaterial, bool returnAsInstance = false)
		{
			foreach(var libraryMaterial in materialLibrary)
            {
                if(comparisonMaterial.name.ToLower().Contains(libraryMaterial.name.ToLower()))
                {
                    Debug.Log("Found library material with matching name: " + libraryMaterial.name);
                    if (returnAsInstance) return Instantiate(libraryMaterial);
                    return libraryMaterial;
				}
                else if (ColorsAreSimilar(comparisonMaterial.GetColor("_BaseColor"),libraryMaterial.GetColor("_BaseColor"), materialColorMatchingThreshold))
                {
                    Debug.Log("Found library material with matching color: " + libraryMaterial.name);
                    if (returnAsInstance) return Instantiate(libraryMaterial);
                    return libraryMaterial;
                }
            }

            //Didnt find a replacement? Just return myself.
            return comparisonMaterial;
		}

        private bool ColorsAreSimilar(Color colorA, Color colorB, float threshold)
        {
            Vector3 colorAVector = new Vector3(colorA.r, colorA.g, colorA.b);
            Vector3 colorBVector = new Vector3(colorB.r, colorB.g, colorB.b);

            if(Vector3.Distance(colorAVector,colorBVector) < threshold)
            {
                return true;
			}
            return false;
		}
	}
}