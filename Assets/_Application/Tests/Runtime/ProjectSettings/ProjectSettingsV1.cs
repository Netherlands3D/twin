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
using System.Transactions;




namespace Netherlands3D.Twin.Tests.Projectsettings
{
    public class ProjectSettingsV1
    {
        
        [UnityOneTimeSetUp]
        public IEnumerator testsetup()
        {
            yield return new WaitForSeconds(1);
            yield return TestFunctions.TestSetup(Path.Combine(Application.persistentDataPath,Application.streamingAssetsPath,"testfiles/projectV1.nl3d"));
            
            yield return TestFunctions.lagenmenuOpenen();
            
            
        }

        [UnityTest]
        public IEnumerator Camerapositie()
        {
            yield return TestFunctions.Cameraposition(new Coordinate(CoordinateSystem.RDNAP,156663.02404785156,463895.14367675781,494.97858460061252));
        }
        [UnityTest]
        public IEnumerator Camerarotatie()
        {
            yield return TestFunctions.CameraRotation(new UnityEngine.Vector3(30,0,0));
        }  
         [UnityTest]
        public IEnumerator DefaultMaaiveldCorrectGeladen()
        {
            yield return TestFunctions.checkGameObjectIsActive(TestFunctions.scene.DefaultMaaiveld,true, "Maaiveld is niet ingeladen");
        } 
        [UnityTest]
        public IEnumerator DefaultGebouwenCorrectGeladen()
        {
            yield return TestFunctions.checkGameObjectIsActive(TestFunctions.scene.DefaultBuildings,true,"gebouwen is niet ingeladen");
        } 

        [UnityTest]
        public IEnumerator DefaultBomenCorrectGeladen()
        {
            yield return TestFunctions.checkGameObjectIsActive(TestFunctions.scene.DefaultBomen,true,"bomen zijn niet ingeladen");
        } 
        [UnityTest]
        public IEnumerator DefaultBossenCorrectGeladen()
        {
            yield return TestFunctions.checkGameObjectIsActive(TestFunctions.scene.DefaultBossen,true, "bossen zijn niet ingeladen");
        }               
        
        [UnityTest]
        public IEnumerator DefaultStraatnamenLaagOpJuistePositieInLagenmenu()
        {
            yield return TestFunctions.LaagCorrectInLagenMenu("Straatnamen",true,true,0);
        } 
        [UnityTest]

        public IEnumerator DefaultBomenLaagOpJuistePositieInLagenmenu()
        {
           yield return TestFunctions.LaagCorrectInLagenMenu("Bomen",true,true,2);
        }
         [UnityTest]
        public IEnumerator DefaultBossenLaagOpJuistePositieInLagenmenu()
        {
            yield return TestFunctions.LaagCorrectInLagenMenu("Bossen",true,true,3);
        } 
        [UnityTest]
        public IEnumerator DefaultGebouwenLaagOpJuistePositieInLagenmenu()
        {
            yield return TestFunctions.LaagCorrectInLagenMenu("Gebouwen",true,true,4);
        } 
        [UnityTest]
        public IEnumerator DefaultMaaiveldLaagOpJuistePositieInLagenmenu()
        {
            yield return TestFunctions.LaagCorrectInLagenMenu("Maaiveld",true,true,5);
        } 
      
        



    }

   

   
}