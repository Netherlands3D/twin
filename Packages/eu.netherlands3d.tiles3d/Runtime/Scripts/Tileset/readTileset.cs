using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.Tiles3D
{
public class readTileset : MonoBehaviour
{
    public string tilesetURL;
        
        public Tileset tileset;
    // Start is called before the first frame update
    void Start()
    {
            tileset = new Tileset(tilesetURL);
        }

      
    }
}
