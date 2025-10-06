using System.Collections;
using Netherlands3D._Application._Twin;
using Netherlands3D.Catalogs.Catalogs;
using UnityEngine;

namespace Netherlands3D
{
    public class ImportPdokCatalog : MonoBehaviour
    {
        public AssetLibrary assetLibrary;
        
        public IEnumerator Start()
        {
            // This is a hack to prevent the boot'n'switch to the config loader script to 
            // only pick this up in the main game loop. The scene switch to the config loader happens in the
            // first frame, and by delaying it one frame this only happens in the 'real' loop
            // Without this hack the PDOK catalog will show up twice because this is added to the scriptable object
            yield return null;
            
            ImportPdokCatalogAsync();
        }

        private async void ImportPdokCatalogAsync()
        {
            assetLibrary.Import(await PdokOgcApiCatalog.CreateAsync("PDOK"));
        }
    }
}
