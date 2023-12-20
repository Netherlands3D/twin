using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class DistanceCalculator : MonoBehaviour
    {
        Camera camera;
        public GameObject targetset;

         GameObject targetGameObject;
        int targetNumber;
        Vector3 targetPostion;
        float targetMargin;
        public UnityEvent<string> onNewTarget;
        public UnityEvent<float> onDistanceChanged;
        public UnityEvent onSucces;
        public UnityEvent onGameFinished;
        // Start is called before the first frame update
        void Start()
        {
            camera = Camera.main;
            startGame();
        }

        public void startGame()
        {
            
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
            if (targetNumber>targetset.transform.childCount)
            {
                onGameFinished.Invoke();
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
            float distance = calculateDistance();
            onDistanceChanged.Invoke(distance);
            if (distance< targetMargin)
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
