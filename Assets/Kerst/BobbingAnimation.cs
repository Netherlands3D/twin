using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Netherlands3D.Twin
{
    public class BobbingAnimation : MonoBehaviour
    {
        public float bobbingHeight = 10f; // Adjust this value to control the bobbing height
        public float bobbingSpeed = 2f; // Adjust this value to control the bobbing speed
        public float horizontalBobbing = 2f; // Adjust this value to control the horizontal offset
        public float smoothness = 5f; // Adjust this value to control the smoothness of interpolation
        float multiplicationFactor = 1f;
        private float vel;

        private Vector3 startPosition;
        private float randomVerticalOffset;
        private float randomHorizontalOffset;
        private flyingCamera cam;

        private float currentFrequency = 1f;
        private bool applyBoost;
        private float frequencyChangeStartTime;
        
        private void Start()
        {
            startPosition = transform.position;
            randomVerticalOffset = Random.Range(0f, 2 * Mathf.PI); // Random vertical offset to avoid synchronization
            randomHorizontalOffset = Random.Range(0f, 2 * Mathf.PI); // Random horizontal offset

            cam = Camera.main.GetComponent<flyingCamera>();
        }

        private void Update()
        {
            if (cam.shouldBoost != applyBoost)
            {
                applyBoost = cam.shouldBoost;
                frequencyChangeStartTime = Time.time;
            }
            
            float newY = startPosition.y + Mathf.Sin((Time.time + randomVerticalOffset) * bobbingSpeed * currentFrequency) * bobbingHeight;
            float newX = startPosition.x + Mathf.Sin((Time.time + randomHorizontalOffset) * bobbingSpeed * currentFrequency) * horizontalBobbing;

            Vector3 targetPosition = new Vector3(newX, newY, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothness);
            
            // Update the frequency smoothly
            float elapsedTime = Time.time - frequencyChangeStartTime;
            float targetFrequency = applyBoost ? 3f : 1f;
            currentFrequency = Mathf.Lerp(currentFrequency, targetFrequency, elapsedTime / cam.jerkTime);
        }
    }
}