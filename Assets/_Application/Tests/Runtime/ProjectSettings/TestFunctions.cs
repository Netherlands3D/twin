using Netherlands3D.E2ETesting;
using UnityEngine;
using System;
using System.Collections;
using Netherlands3D.Coordinates;
using UnityEngine.TestTools;
using NUnit.Framework;
using Netherlands3D.Twin.Projects;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Tests.Projectsettings
{
     public static class TestFunctions
    {
        public static Scene scene;
        private static bool projectIsLoaded;
        public static IEnumerator Cameraposition(Coordinate correctAnswer)
        {
            UnityEngine.Vector3 cameraposition = Camera.main.transform.position;
            Coordinate campos = new Coordinate(cameraposition).Convert(CoordinateSystem.RDNAP);


           
            double deltaEasting = Math.Abs(campos.easting - correctAnswer.easting);
            bool deltaEastingCorrect = deltaEasting<0.001;
            E2E.Then(deltaEastingCorrect , Is.EqualTo(true) , "cameraposition Easting incorrect");

            double deltaNorthing = Math.Abs(campos.northing - correctAnswer.northing);
            bool deltaNorthingCorrect = deltaNorthing<0.001;
            E2E.Then(deltaNorthingCorrect , Is.EqualTo(true) , "cameraposition Northing incorrect");

            double deltaHeight = Math.Abs(campos.height - correctAnswer.height);
            bool deltaHeightCorrect = deltaHeight<0.001;
            E2E.Then(deltaHeightCorrect , Is.EqualTo(true) , "cameraposition incorrect");
            yield return null;
        }

        public static IEnumerator CameraRotation(UnityEngine.Vector3 correctAnswer)
        {
            UnityEngine.Vector3 camerarotatie = Camera.main.transform.eulerAngles;
            double deltaRotationEasting = Math.Abs(camerarotatie.x-correctAnswer.x);
            bool deltaRotationEastingCorrect = deltaRotationEasting<0.001;
            E2E.Then(deltaRotationEastingCorrect,Is.EqualTo(true),"camerarotation-x incorrect");

            double deltaRotationNorthing = Math.Abs(camerarotatie.y-correctAnswer.y);
            bool deltaRotationNorthingCorrect = deltaRotationNorthing<0.001;
            E2E.Then(deltaRotationNorthingCorrect,Is.EqualTo(true),"camerarotation-y incorrect");

            double deltaRotationHeight = Math.Abs(camerarotatie.z-correctAnswer.z);
            bool deltaRotationHeightCorrect = deltaRotationHeight<0.001;
            E2E.Then(deltaRotationHeightCorrect,Is.EqualTo(true),"camerarotation-z incorrect");
            yield return null;

        }
        public static IEnumerator TestSetup(string projectfilepath)
        {
            yield return E2E.EnsureMainSceneIsLoaded();
            yield return new WaitForSeconds(2);
            //this.Sidebar = new Sidebar();
            scene = new Scene();
            
           ProjectDataHandler pdh  =(ProjectDataHandler)E2E.Find("ProjectDataHandler").Component<ProjectDataHandler>();
           projectIsLoaded=false;
           pdh.OnLoadCompleted.AddListener(ReCeiveProjectLoadedEvent);
        
            pdh.LoadFromFile(projectfilepath);
           while( projectIsLoaded==false)
            {
                yield return null;
            }
            
        }
        private static void ReCeiveProjectLoadedEvent()
        {
            projectIsLoaded=true;   
        }
        
        public static IEnumerator checkGameObjectDoesNotExist(Netherlands3D.E2ETesting.PageObjectModel.Element<UnityEngine.GameObject> testSubject, string failmessage)
        {
            yield return E2E.Expect(()=>testSubject,Is.EqualTo(null),10,failmessage);
        }
        public static IEnumerator checkGameObjectIsActive(Netherlands3D.E2ETesting.PageObjectModel.Element<UnityEngine.GameObject> testSubject,bool shouldBeActive,string failMessage="")
        {
            yield return E2E.Expect(()=>testSubject.IsActive,Is.EqualTo(shouldBeActive),20,failMessage);
        }
        public static IEnumerator lagenmenuOpenen()
        {
            UnityEngine.GameObject lagenmenuButton = (UnityEngine.GameObject)scene.LagenMenuButton;
            UnityEngine.UI.Button button = lagenmenuButton.GetComponent<Button>();
            button.onClick.Invoke();
            yield return checkGameObjectIsActive(scene.Layerspanel,true);

            
        }
    
        public static IEnumerator LaagCorrectInLagenMenu(string laagnaam,bool shouldBeInList, bool shouldBeActive=true, int laagindex=-1)
        {
            UnityEngine.GameObject layerspanel= (UnityEngine.GameObject)scene.Layerspanel;
            UnityEngine.GameObject laagobject=null;
            if(laagindex==-1)
            {
            laagobject = layerspanel.transform.Find(laagnaam).gameObject;
            bool laagobjectAanwezig = laagobject!=null;
             E2E.Then(laagobjectAanwezig,Is.EqualTo(shouldBeInList),laagnaam +"niet correct in lagenpaneel");
            }
            else
            {
                 E2E.Then(laagindex,Is.AtMost(layerspanel.transform.childCount-1),laagnaam +" niet op correcte positie in lagenpaneel");
                laagobject = layerspanel.transform.GetChild(laagindex).gameObject;
                E2E.Then(laagobject.name,Is.EqualTo(laagnaam),laagnaam +" niet op correcte positie in lagenpaneel");
            }

            UnityEngine.GameObject enableToggleObject = laagobject.transform.Find("ParentRow/EnableToggle").gameObject;
            UnityEngine.UI.Toggle Toggle = enableToggleObject.GetComponent<Toggle>();
            bool ToggleValue = Toggle.isOn;
            E2E.Then(ToggleValue,Is.EqualTo(shouldBeActive),laagnaam +" isActive niet correct in lagenpaneel");
            yield return null;
        }


    }
}
