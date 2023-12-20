using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class DistanceCalculator : MonoBehaviour
    {

        public List<GameObject> targetLists = new List<GameObject>();

        bool isActive = false;
        Camera camera;
        GameObject targetset;

        GameObject targetGameObject;
        public int targetNumber;
        public string targetName;
        Vector3 targetPostion;
        float targetMargin;
        public int targetcount;
        public UnityEvent<string> onNewTarget;
        public UnityEvent<float> onDistanceToTargetChanged;
        public UnityEvent<float> kmTravelled;
        public UnityEvent<int> onTargetReached;
        public UnityEvent onGameFinished;
        float distanceTravelled;
        Vector3 oldCameraposition;
        // Start is called before the first frame update
        void Awake()
        {
            camera = Camera.main;
            //startGame(0);
        }

        public void startGame(int targetListID)
        {
            if (targetLists.Count > targetListID)
            {
                targetset = targetLists[targetListID];
            }
            targetcount = targetset.transform.childCount - 1;

            isActive = true;
            distanceTravelled = 0;
            oldCameraposition = camera.transform.position;

            targetGameObject = targetset.transform.GetChild(0).gameObject;

            camera.transform.position = targetGameObject.transform.position;
            camera.transform.rotation = targetGameObject.transform.rotation;

            targetNumber = 1;
            targetGameObject = targetset.transform.GetChild(1).gameObject;
            targetName = targetGameObject.name;
            targetPostion = targetGameObject.transform.position;
            targetPostion.y = 0;
            targetMargin = targetGameObject.transform.localScale.x;
            onNewTarget.Invoke(targetGameObject.name);
        }

        public void NextTarget()
        {
            isActive = true;
            targetNumber++;
           
            targetGameObject = targetset.transform.GetChild(targetNumber).gameObject;
            targetName = targetGameObject.name;
            targetPostion = targetGameObject.transform.position;
            targetPostion.y = 0;

            targetMargin = targetGameObject.transform.localScale.x;
            onNewTarget.Invoke(targetGameObject.name);
        }

        public void TargetReached()
        {


            isActive = false;
            targetGameObject.SetActive(false);
            if (targetNumber > targetset.transform.childCount-1)
            {
                onGameFinished.Invoke();
                isActive = false;
                return;
            }
            onTargetReached.Invoke(targetNumber);

        }

        // Update is called once per frame
        void Update()
        {
            if (isActive == false)
            {
                return;
            }
            float distance = calculateDistance();
            onDistanceToTargetChanged.Invoke(distance);
            Vector3 newCameraposition = camera.transform.position;
            newCameraposition.y = 0;
            distanceTravelled += Vector3.Distance(oldCameraposition, newCameraposition);
            oldCameraposition = newCameraposition;
            kmTravelled.Invoke(distanceTravelled / 1000);
            if (distance < targetMargin)
            {
                TargetReached();
            }
        }

        float calculateDistance()
        {
            Vector3 campos2D = camera.transform.position;
            campos2D.y = 0; ;
            return Vector3.Distance(campos2D, targetPostion);
        }
    }
}
