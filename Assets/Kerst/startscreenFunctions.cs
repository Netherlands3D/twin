using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class startscreenFunctions : MonoBehaviour
    {
        public GameManager gameManager;
        public void startHoofdsteden()
        {
            int setindex = Mathf.FloorToInt(Random.Range(0f, 3f));
            gameManager.StartGame(setindex);
        }

        public void startSteden()
        {
            int setindex = 3+Mathf.FloorToInt(Random.Range(0f, 3f));
            gameManager.StartGame(setindex);
        }
        
    }
}
