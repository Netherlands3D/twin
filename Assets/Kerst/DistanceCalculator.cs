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
        int targetNumber;
        Vector3 targetPostion;
        float targetMargin;
        public UnityEvent<string> onNewTarget;
        public UnityEvent<float> onDistanceToTargetChanged;
        public UnityEvent<float> kmTravelled;
        public UnityEvent onSucces;
        public UnityEvent onGameFinished;
        float distanceTravelled;
        Vector3 oldCameraposition;
        // Start is called before the first frame update
        void Start()
        {
            camera = Camera.main;
            startGame(0);
        }

        public void startGame(int targetListID)
        {
            if (targetLists.Count > targetListID)
            {
                targetset = targetLists[targetListID];
            }

            isActive = true;
            distanceTravelled = 0;
            oldCameraposition = camera.transform.position;

            targetGameObject = targetset.transform.GetChild(0).gameObject;

            camera.transform.position = targetGameObject.transform.position;
            camera.transform.rotation = targetGameObject.transform.rotation;

            targetNumber = 1;
            targetGameObject = targetset.transform.GetChild(1).gameObject;
            targetPostion = targetGameObject.transform.position;
            targetPostion.y = 0;
            targetMargin = targetGameObject.transform.localScale.x;
            onNewTarget.Invoke(targetGameObject.name);
        }

        public void onTargetReached()
        {
            targetNumber++;
            targetGameObject.SetActive(false);
            if (targetNumber > targetset.transform.childCount)
            {
                onGameFinished.Invoke();
                isActive = false;
                return;
            }




            Transform nextTransform = targetset.transform.GetChild(targetNumber);
            targetGameObject = nextTransform.gameObject;
            targetPostion = targetGameObject.transform.position;
            targetPostion.y = 0;

            targetMargin = targetGameObject.transform.localScale.x;
            onNewTarget.Invoke(targetGameObject.name);
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
                onSucces.Invoke();
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
