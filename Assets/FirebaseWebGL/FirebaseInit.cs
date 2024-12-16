using System.Collections;
using System.Collections.Generic;
using FirebaseWebGL.Database;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class FirebaseInit : MonoBehaviour
    {
        public static string userId;

        private void Start()
        {
            FirebaseAuth.OnAuthStateChanged(gameObject.name, "DisplayUserInfo", "DisplayInfo");

            Debug.Log("SIGNING IN ANONYMOUSLY");
            FirebaseAuth.SignInAnonymously(gameObject.name, "DisplayData", "DisplayError");

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

        public void DisplayUserInfo(string user)
        {
            var parsedUser = JsonConvert.DeserializeObject<FirebaseUser>(user);
            DisplayData($"Email: {parsedUser.email}, UserId: {parsedUser.uid}, EmailVerified: {parsedUser.isEmailVerified}");

            userId = parsedUser.uid;
        }

        public void DisplayInfo(string info)
        {
            Debug.Log("DISPLAY INFO" + info);
        }
    }
}