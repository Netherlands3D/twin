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

        private Vector3 startPosition;
        private float randomVerticalOffset;
        private float randomHorizontalOffset;

        private void Start()
        {
            startPosition = transform.position;
            randomVerticalOffset = Random.Range(0f, 2*Mathf.PI); // Random vertical offset to avoid synchronization
            randomHorizontalOffset = Random.Range(0f, 2*Mathf.PI); // Random horizontal offset
        }

        private void Update()
        {
            float newY = startPosition.y + Mathf.Sin((Time.time + randomVerticalOffset) * bobbingSpeed) * bobbingHeight;
            float newX = startPosition.x + Mathf.Sin((Time.time + randomHorizontalOffset) * bobbingSpeed) * horizontalBobbing;

            transform.position = new Vector3(newX, newY, transform.position.z);
        }
    }

}