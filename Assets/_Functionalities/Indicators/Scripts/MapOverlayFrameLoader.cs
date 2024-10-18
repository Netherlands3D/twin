using System;
using Netherlands3D.Indicators.Dossiers;
using UnityEngine;

namespace Netherlands3D.Indicators
{
    public class MapOverlayFrameLoader : MonoBehaviour
    {
        [SerializeField] private DossierSO dossier;
        [SerializeField] private GameObject decalProjector;
        [SerializeField] private RemoteTextureLoader textureLoader;


        private void Awake()
        {
            dossier.onSelectedDataLayer.AddListener(ToggleProjector);
            dossier.onLoadMapOverlayFrame.AddListener(LoadTexture);
        }

        private void ToggleProjector(DataLayer? dataLayer)
        {
            decalProjector.SetActive(dataLayer.HasValue);
        }

        private void LoadTexture(Uri mapUri)
        {
            textureLoader.Load(mapUri);
        }
    }
}