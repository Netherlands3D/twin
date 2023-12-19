using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class DistanceCalculator : MonoBehaviour
    {
        Camera camera;
        public GameObject targetGameObject;
        Vector2 targetPostion;
        public UnityEvent onGameStarted;
        public UnityEvent<float> onDistanceChanged;
        // Start is called before the first frame update
        void Start()
        {
            camera = Camera.main;
            targetPostion = (Vector2)targetGameObject.transform.position;
            onGameStarted.Invoke();
        }

        // Update is called once per frame
        void Update()
        {
            onDistanceChanged.Invoke(calculateDistance());
        }

        float calculateDistance()
        {
            return Vector2.Distance((Vector2)camera.transform.position, targetPostion);
        }
    }
}
