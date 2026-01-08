using System;
using System.Collections;
using System.Diagnostics;
using System.Numerics;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Tests.PageObjectModel;

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;


namespace Netherlands3D.Twin.Tests
{
    public class DefaultTerreinLayer
    {    
        public Scene Scene { get; private set; }
        private bool projectIsLoaded;

    [UnitySetUp]
    public IEnumerator TestSetup()
        {
            yield return E2E.EnsureMainSceneIsLoaded();
            
            //this.Sidebar = new Sidebar();
            this.Scene = new Scene();
            
           ProjectDataHandler pdh  =(ProjectDataHandler)Scene.projectdataHandler.Component<ProjectDataHandler>();
           projectIsLoaded=false;
           pdh.OnLoadCompleted.AddListener(ReCeivoProjectLoadedEvent);

            pdh.LoadFromFile(Application.persistentDataPath+"/maaiveldUit.nl3d");
           while( projectIsLoaded==false)
            {
                yield return null;
            }
            
        }
    
        private void ReCeivoProjectLoadedEvent()
        {
            projectIsLoaded=true;   
        }

        [UnityTest]

        [Category("Visible")]
        public IEnumerator TerrainLayerShouldBeInVisible()
        {
           // Sidebar.LayerPanelShouldBeOpen();

           // var terrainLayer = Sidebar.Inspectors.Layers.Maaiveld;

            //E2E.Then(terrainLayer.Visibility.IsOn);
            //E2E.Then(terrainLayer.IsActive);
           E2E.Then (Scene.DefaultMaaiveld.IsActive,Is.False,"maaiveldlaag is actief");
           yield return null;
            //BinaryMeshLayer bml  =(BinaryMeshLayer)E2E.FindComponentOnGameObject<BinaryMeshLayer>("Functionalities/CartesianTiles/Maaiveld");
            //bool isenabled = bml.isEnabled;
            //E2E.Then(isenabled,Is.False);
            
        }

       

    }
}