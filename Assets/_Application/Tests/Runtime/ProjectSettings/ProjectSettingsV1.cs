using System;
using System.Collections;
using System.Diagnostics;
using System.Numerics;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Netherlands3D.Twin.Projects;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Netherlands3D.E2ETesting.PageObjectModel;
using Netherlands3D.E2ETesting;


namespace Netherlands3D.Twin.Tests.Projectsettings
{
    public class ProjectSettingsV1
    {
        
        [UnitySetUp]
        public IEnumerator testsetup()
        {
            yield return TestFunctions.TestSetup(Application.persistentDataPath+"/projectV1.nl3d");
        }

        [UnityTest]
        public IEnumerator testCameraPosition()
        {
            yield return TestFunctions.Cameraposition(new Coordinate(CoordinateSystem.RDNAP,156663.02404785156,463895.14367675781,494.97858460061252));
        }

        [UnityTest]
        public IEnumerator terreinLayerIsLoaded()
        {
            yield return TestFunctions.checkGameObject(TestFunctions.scene.DefaultMaaiveld);
        }  
        [UnityTest]
        public IEnumerator BuildingLayerIsLoaded()
        {
            yield return TestFunctions.checkGameObject(TestFunctions.scene.DefaultBuildings);
        } 
    }

    public class Scene : Element
    {
        public Element<GameObject> DefaultMaaiveld => E2E.Find("Functionalities/CartesianTiles/Maaiveld (Clone)");
        public Element<GameObject> DefaultBuildings => E2E.Find("Functionalities/CartesianTiles/Gebouwen (Clone)");

         public Element<GameObject> projectdataHandler => E2E.Find("ProjectDataHandler");
    }

    public static class TestFunctions
    {
        public static Scene scene;
        private static bool projectIsLoaded;
        public static IEnumerator Cameraposition(Coordinate correctAnswer)
        {
            UnityEngine.Vector3 cameraposition = Camera.main.transform.position;
            Coordinate campos = new Coordinate(cameraposition);
            campos.Convert(CoordinateSystem.RDNAP);

           
            double deltaEasting = Math.Abs(campos.easting-correctAnswer.easting);
            bool deltaEastingCorrect = deltaEasting<0.001;
            E2E.Then(deltaEastingCorrect,Is.EqualTo(true),"cameraposition Easting incorrect");

            double deltaNorthing = Math.Abs(campos.northing-correctAnswer.northing);
            bool deltaNorthingCorrect = deltaNorthing<0.001;
            E2E.Then(deltaNorthingCorrect,Is.EqualTo(true),"cameraposition Northing incorrect");

            double deltaHeight = Math.Abs(campos.height-correctAnswer.height);
            bool deltaHeightCorrect = deltaHeight<0.001;
            E2E.Then(deltaHeightCorrect,Is.EqualTo(true),"cameraposition incorrect");
            yield return null;
        }
    public static IEnumerator TestSetup(string projectfilepath)
        {
            yield return E2E.EnsureMainSceneIsLoaded();
            
            //this.Sidebar = new Sidebar();
            scene = new Scene();
            
           ProjectDataHandler pdh  =(ProjectDataHandler)E2E.Find("ProjectDataHandler").Component<ProjectDataHandler>();
           projectIsLoaded=false;
           pdh.OnLoadCompleted.AddListener(ReCeivoProjectLoadedEvent);

            pdh.LoadFromFile(projectfilepath);
           while( projectIsLoaded==false)
            {
                yield return null;
            }
            
        }
        private static void ReCeivoProjectLoadedEvent()
        {
            projectIsLoaded=true;   
        }
        public static IEnumerator checkGameObject(Netherlands3D.E2ETesting.PageObjectModel.Element<UnityEngine.GameObject> testSubject)
        {
            yield return E2E.Expect(()=>testSubject.IsActive,Is.True,20,"");
        }
    }
}