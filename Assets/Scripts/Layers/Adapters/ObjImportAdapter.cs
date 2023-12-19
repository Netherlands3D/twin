using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/OBJImportAdapter", fileName = "OBJImportAdapter", order = 0)]
    public class ObjImportAdapter : ScriptableObject
    {
     [Header("Required input")]
        [SerializeField] Material baseMaterial;

        [Header("Settings")]
        [SerializeField] bool createSubMeshes = false;
        
        [Header("Result")] public UnityEvent<GameObject> CreatedMoveableGameObject;
        // public UnityEvent<GameObject> CreatedImmoveableGameObject;

        // [Header("Progress")][SerializeField] UnityEvent<bool> busy;
        // public UnityEvent<string> currentActivity;
        // public UnityEvent<string> currentAction;
        // public UnityEvent<float> progressPercentage;


        // [Header("Alerts and errors")]
        // public UnityEvent<string> alertmessage;
        // public UnityEvent<string> errormessage;

        string objFileName = "";

        string objfilename
        {
            get { return objFileName; }
            set
            {
                objFileName = value;

                Debug.Log("received objFile: " + objFileName);

            }
        }

        string mtlFileName = "";

        string mtlfilename
        {
            get { return mtlFileName; }
            set { mtlFileName = value; }
        }



        string imgFileName = "";

        string imgfilename
        {
            get { return imgFileName; }
            set
            {
                imgFileName = value;

                //if (ReceivedImageFilename) ReceivedImageFilename.InvokeStarted(System.IO.Path.GetFileName(imgFileName));
            }
        }

        ObjImporter.ObjImporter importer;

        public void ParseFiles(string value)
        {
            Debug.Log("receiveid files: " + value);

            if (value == "") //empty string received, so no selection was made
            {
                return;
            }

            string[] filenames = value.Split(',');

            foreach (var file in filenames)
            {
                string fileextention = System.IO.Path.GetExtension(file).ToLower();
                switch (fileextention)
                {
                    case ".obj":
                        objfilename = System.IO.Path.Combine(Application.persistentDataPath, file);
                        break;

                    case ".mtl":
                        mtlfilename = System.IO.Path.Combine(Application.persistentDataPath, file);
                        break;

                    case ".jpg":
                    case ".png":
                    case ".jpeg":
                        imgfilename = System.IO.Path.Combine(Application.persistentDataPath, file);
                        break;
                }
            }

            if (objfilename != "")
            {

                OnStartImporting();

            }
        }

        void OnOBJFileReceived(string value)
        {
            objfilename = value;
        }

        void OnMTLFileReceived(string value)
        {
            mtlfilename = value;
        }

        public void SetImageFile(string value)
        {
            imgfilename = value;
        }



        public void Cancel()
        {
            // BroadcastMessage("Cancel");
            // currentActivity.Invoke("cancelling the import");
        }

        void OnStartImporting()
        {
            ConnectToImporter();

            importer.objFilePath = objfilename;
            importer.mtlFilePath = mtlfilename;
            importer.imgFilePath = imgfilename;

            importer.BaseMaterial = baseMaterial;
            importer.createSubMeshes = createSubMeshes;
            // busy.Invoke(true);
            importer.StartImporting(OnOBJImported);
        }

        void OnOBJImported(GameObject returnedGameObject)
        {
            // bool canBemoved = importer.createdGameobjectIsMoveable;

            // busy.Invoke(false);


            objfilename = string.Empty;
            mtlfilename = string.Empty;
            imgfilename = string.Empty;

            if (importer != null) Destroy(importer.gameObject);

            // if (canBemoved)
            // {
                AddLayerScriptToObj(returnedGameObject);
                // }
                // else
                // {
                //     CreatedImmoveableGameObject.Invoke(returnedGameObject);
                // }
        }

        void ConnectToImporter()
        {
            if (importer != null) Destroy(importer.gameObject);

            importer = new GameObject().AddComponent<ObjImporter.ObjImporter>();
            // give the importer handles for progress- and errormessaging
            importer.currentActivity = BroadcastCurrentActivity;
            importer.currentAction = BroadcastCurrentAction;
            importer.progressPercentage = BroadcastProgressPercentage;
            importer.alertmessage = BroadcastAlertmessage;
            importer.errormessage = BroadcastErrormessage;

            Debug.Log("Connected to new ObjImporter");
        }

        void BroadcastCurrentActivity(string value)
        {
            // currentActivity.Invoke(value);
        }

        void BroadcastCurrentAction(string value)
        {
            // currentAction.Invoke(value);
        }

        void BroadcastProgressPercentage(float value)
        {
            // progressPercentage.Invoke(value);
        }

        void BroadcastAlertmessage(string value)
        {
            // alertmessage.Invoke(value);
        }

        void BroadcastErrormessage(string value)
        {
            // errormessage.Invoke(value);
        }
        
        public void AddLayerScriptToObj(GameObject parsedObj)
        {
            var spawnPoint = ObjectPlacementUtility.GetSpawnPoint();

            parsedObj.transform.position = spawnPoint;
            
            var objLayer = parsedObj.AddComponent<ObjectLayer>();
            parsedObj.AddComponent<MeshCollider>();
            objLayer.UI.Select();
            CreatedMoveableGameObject.Invoke(parsedObj);
        }
    }
}
