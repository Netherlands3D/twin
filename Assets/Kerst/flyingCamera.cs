using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class flyingCamera : MonoBehaviour
    {
        [HideInInspector] public float rotationSpeed = 0;
        public float maxRotationSpeed = 1;
        [HideInInspector] public float angularAcceleration = 0;
        public float rotationJerkTime = 0.2f;
        [HideInInspector] public float moveSpeed = 0;
        public float maxSpeed = 4000;
        [HideInInspector] public float acceleration = 0;
        public float jerkTime = 0.3f;

        public float maxBreakTimer = 5f;
        float breakTimer;
        public float maxBoostTimer = 5f;
        float boostTimer;
        public float boostMultiplier = 2f;

        // public ContextMenuButton leftbutton;

        public bool shouldMove = false;
        public bool shouldBreak = false;
        public bool shouldBoost = false;
        public bool turnLeft = false;
        public bool turnRight = false;

        private void Start()
        {
            breakTimer = maxBreakTimer;
            boostTimer = maxBoostTimer;
        }

        // Update is called once per frame
        void Update()
        {
            keyCapture();
            CalculateBoostAndBreak();
            CalculateRotation();
            CalculateMovement();
        }

        private void CalculateBoostAndBreak()
        {
            if (shouldBoost)
                boostTimer -= Time.deltaTime;
            else
                boostTimer += Time.deltaTime;
            boostTimer = Mathf.Clamp(boostTimer, 0, maxBoostTimer);
            
            if (shouldBreak)
                breakTimer -= Time.deltaTime;
            else
                breakTimer += Time.deltaTime;
            breakTimer = Mathf.Clamp(breakTimer, 0, maxBreakTimer);
        }

        private void CalculateMovement()
        {
            shouldMove = !shouldBreak || breakTimer <= 0;
            var maxSpeedMultiplier = 1f;
            if (shouldBoost)// && boostTimer > 0) //infinite boost allowed
                maxSpeedMultiplier = boostMultiplier;
            

            if (shouldMove)
                moveSpeed = Mathf.SmoothDamp(moveSpeed, maxSpeed * maxSpeedMultiplier, ref acceleration, jerkTime);
            else
                moveSpeed = Mathf.SmoothDamp(moveSpeed, 0, ref acceleration, jerkTime);


            Vector3 forwardFlat = transform.forward.normalized;
            forwardFlat.y = 0;

            transform.position += forwardFlat * moveSpeed * Time.deltaTime;
        }

        private void CalculateRotation()
        {
            if (turnLeft)
            {
                rotationSpeed = Mathf.SmoothDamp(rotationSpeed, -maxRotationSpeed, ref angularAcceleration, rotationJerkTime);
            }
            else if (turnRight)
            {
                rotationSpeed = Mathf.SmoothDamp(rotationSpeed, maxRotationSpeed, ref angularAcceleration, rotationJerkTime);
            }
            else
            {
                rotationSpeed = Mathf.SmoothDamp(rotationSpeed, 0, ref angularAcceleration, rotationJerkTime);
            }

            transform.RotateAround(Vector3.up, Time.deltaTime * rotationSpeed);
        }

        void keyCapture()
        {
            //turn left
            if (Input.GetKeyDown(KeyCode.LeftArrow)) turnLeft = true;

            if (Input.GetKeyUp(KeyCode.LeftArrow)) turnLeft = false;

            if (Input.GetKeyDown(KeyCode.Q)) turnLeft = true;
            if (Input.GetKeyUp(KeyCode.Q)) turnLeft = false;

            //turn right
            if (Input.GetKeyDown(KeyCode.RightArrow)) turnRight = true;
            if (Input.GetKeyUp(KeyCode.RightArrow)) turnRight = false;
            if (Input.GetKeyDown(KeyCode.E)) turnRight = true;
            if (Input.GetKeyUp(KeyCode.E)) turnRight = false;

            //move
            if (Input.GetKeyDown(KeyCode.UpArrow)) shouldBoost = true;
            if (Input.GetKeyUp(KeyCode.UpArrow)) shouldBoost = false;
            if (Input.GetKeyDown(KeyCode.W)) shouldBoost = true;
            if (Input.GetKeyUp(KeyCode.W)) shouldBoost = false;
            
            //break
            if (Input.GetKeyDown(KeyCode.DownArrow)) shouldBreak = true;
            if (Input.GetKeyUp(KeyCode.DownArrow)) shouldBreak = false;
            if (Input.GetKeyDown(KeyCode.S)) shouldBreak = true;
            if (Input.GetKeyUp(KeyCode.S)) shouldBreak = false;
        }
    }
}