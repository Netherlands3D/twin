using System.Collections;
using System.Collections.Generic;
using FirebaseWebGL.Database;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class FirebaseInit : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("GETTING FIREBASE DATA");
            FirebaseDatabase.GetJSON("test/path", gameObject.name, "DisplayData", "DisplayError");
        }

        public void DisplayData(string data)
        {            
            Debug.Log("DISPLAYING FIREBASEDATA" + data);
        }

        public void DisplayError(string error)
        {
            Debug.Log("DISPLAYING FIREBASEERROR" + error);
        }
    }
}

////the following needs to be in the index.html:
//////////////////////////////////////////////

//< script src = "https://www.gstatic.com/firebasejs/8.8.0/firebase.js" ></ script >
//< script > 

//const firebaseConfig = {
//  apiKey: "AIzaSyDlYdgMb-zINsNixTdfrLGmqxiOCIZ95E8",
//  authDomain: "kerst2024-6c596.firebaseapp.com",
//  databaseURL: "https://kerst2024-6c596-default-rtdb.europe-west1.firebasedatabase.app",
//  projectId: "kerst2024-6c596",  
//  messagingSenderId: "839108749676",
//  appId: "1:839108749676:web:26ab8b6a2775b683c0eb49"
//};

//firebase.initializeApp(firebaseConfig);
//</ script >


////then find the script.onload function and add the window instance and the firebase instance and change it to:
///////////////////////////////////////////////

//script.onload = () => {
//    createUnityInstance(canvas, config, (progress) => {
//        progressBarFull.style.width = 100 * progress + "%";
//    }).then((instance) => {
//        unityInstance = instance;
//        window.unityInstance = instance;
//        this.firebase = firebase;
//        loadingBar.style.display = "none";
//        loadingBar.remove();
//    }).catch((message) => {
//        alert(message);
//    });
//};