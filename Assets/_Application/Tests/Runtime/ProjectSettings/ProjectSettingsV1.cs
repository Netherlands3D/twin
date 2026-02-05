using System;
using System.Collections;
using System.Diagnostics;
using System.Numerics;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using Netherlands3D.Twin.Projects;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Netherlands3D.E2ETesting.PageObjectModel;
using Netherlands3D.E2ETesting;
using System.IO;
using System.Linq.Expressions;
using System.Net.NetworkInformation;



namespace Netherlands3D.Twin.Tests.Projectsettings
{
    public class ProjectSettingsV1
    {
        
        [UnitySetUp]
        public IEnumerator testsetup()
        {
            yield return new WaitForSeconds(1);
            yield return TestFunctions.TestSetup(Path.Combine(Application.persistentDataPath,Application.streamingAssetsPath,"testfiles/projectV1.nl3d"));
        }


        [UnityTest]
        public IEnumerator Version1()
        {
            yield return TestFunctions.lagenmenuOpenen();
            yield return TestFunctions.Cameraposition(new Coordinate(CoordinateSystem.RDNAP,156663.02404785156,463895.14367675781,494.97858460061252));
            yield return TestFunctions.CameraRotation(new UnityEngine.Vector3(30,0,0));
            yield return TestFunctions.checkGameObject(TestFunctions.scene.DefaultMaaiveld, "Maaiveld is niet ingeladen");
            yield return TestFunctions.checkGameObject(TestFunctions.scene.DefaultBuildings,"gebouwen is niet ingeladen");
            yield return TestFunctions.checkGameObject(TestFunctions.scene.DefaultBomen,"bomen zijn niet ingeladen");
            yield return TestFunctions.checkGameObject(TestFunctions.scene.DefaultBossen, "bossen zijn niet ingeladen");
            yield return TestFunctions.LaagCorrectInLagenMenu("Bomen",true,true);
            

        }
        



    }

    public class Scene : Element
    {
        public Element<GameObject> DefaultMaaiveld => E2E.Find("Functionalities/CartesianTiles/Maaiveld (Clone)");
        public Element<GameObject> DefaultBuildings => E2E.Find("Functionalities/CartesianTiles/Gebouwen (Clone)");
        public Element<GameObject> DefaultBomen => E2E.Find("Functionalities/CartesianTiles/Bomen (Clone)");
        public Element<GameObject> DefaultBossen => E2E.Find("Functionalities/CartesianTiles/Bossen (Clone)");
        public Element<GameObject> LagenMenuButton => E2E.Find("CanvasUI/Base/Body/Sidebar & Toolbar/Toolbar/TopButtons/ToolbarButton_Layers");
        public Element<GameObject> Layerspanel => E2E.Find("CanvasUI/Base/Body/Sidebar & Toolbar/Sidebar/Content/LayersInspector(Clone)/LayersPanel/Layers");

    }

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
        public static IEnumerator checkGameObject(Netherlands3D.E2ETesting.PageObjectModel.Element<UnityEngine.GameObject> testSubject,string failMessage="")
        {
            yield return E2E.Expect(()=>testSubject.IsActive,Is.True,20,failMessage);
        }
        public static IEnumerator lagenmenuOpenen()
        {
            UnityEngine.GameObject lagenmenuButton = (UnityEngine.GameObject)scene.LagenMenuButton;
            UnityEngine.UI.Button button = lagenmenuButton.GetComponent<Button>();
            button.onClick.Invoke();
            yield return checkGameObject(scene.Layerspanel);
            
        }
    
        public static IEnumerator LaagCorrectInLagenMenu(string laagnaam,bool shouldBeInList, bool shouldBeActive=true)
        {
            UnityEngine.GameObject layerspanel= (UnityEngine.GameObject)scene.Layerspanel;
            UnityEngine.GameObject laagobject = layerspanel.transform.Find(laagnaam).gameObject;

            bool laagobjectAanwezig = laagobject!=null;
            E2E.Then(laagobjectAanwezig,Is.EqualTo(shouldBeInList),laagnaam +"niet correct in lagenpaneel");

            UnityEngine.GameObject enableToggleObject = laagobject.transform.Find("ParentRow/EnableToggle").gameObject;
            UnityEngine.UI.Toggle Toggle = enableToggleObject.GetComponent<Toggle>();
            bool ToggleValue = Toggle.isOn;
            E2E.Then(ToggleValue,Is.EqualTo(shouldBeActive),laagnaam +" isActive niet correct in lagenpaneel");
            yield return null;
        }
    }
}