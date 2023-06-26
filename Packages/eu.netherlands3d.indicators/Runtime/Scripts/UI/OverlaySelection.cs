using System.Collections.Generic;
using Netherlands3D.Rendering;
using Netherlands3D.TileSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands.Indicators.UI
{
    public class OverlaySelection : MonoBehaviour
    {
        [SerializeField] private Toggle overlayRadioOptionPrefab;
        [SerializeField] private RemoteTextureLoader remoteTextureLoader;
        [SerializeField] private TextureDecalProjector textureProjector;
        [SerializeField] private List<BinaryMeshLayer> disableShadowsOnLayers = new();
        
        private void Awake()
        {
            Clear();
        }

        public void Render(Dossiers dossiers)
        {
            Debug.Log("Start Rendering");
            Clear();
            if (dossiers.ActiveVariant.HasValue == false) return;

            foreach (var map in dossiers.ActiveVariant.Value.maps)
            {
                var overlayName = map.Value.name;
                var overlayUrl = map.Value.frames[0].map;
                Debug.Log("Will it toggle?");
                Debug.Log(overlayRadioOptionPrefab);

                Toggle option = Instantiate(overlayRadioOptionPrefab.gameObject, Vector3.zero, Quaternion.identity, transform).GetComponent<Toggle>();
                Debug.Log("It will toggle!");
                Debug.Log(overlayName);
                Debug.Log(overlayUrl);
                option.GetComponentInChildren<Text>().text = overlayName;
                option.onValueChanged.AddListener(
                    (value) =>
                    {
                        textureProjector.gameObject.SetActive(value);

                        foreach (var meshLayer in disableShadowsOnLayers)
                        {
                            meshLayer.EnableShadows(!value);
                        }

                        if (value)
                        {
                            remoteTextureLoader.url = overlayUrl;
                            remoteTextureLoader.Load();
                            return;
                        }

                        textureProjector.ClearTexture();
                    }
                );
            }
        }

        public void Clear()
        {
            transform.ClearAllChildren();
        }
    }
}
