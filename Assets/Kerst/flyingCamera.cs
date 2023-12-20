using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class flyingCamera : MonoBehaviour
    {
        public float rotationspeed = 1;
        public float movespeed = 0;
        public float maxSpeed = 4000;
        public float acceleration = 0;
        public float maxBreakTimer = 5f;
        float breakTimer;
        public float jerkTime = 0.3f;

        public ContextMenuButton leftbutton;

        public bool shouldMove = false;
        public bool shouldBreak = false;
        public bool turnLeft = false;
        public bool turnRight = false;

        private void Start()
        {
            breakTimer = maxBreakTimer;
        }

        // Update is called once per frame
        void Update()
        {
            keyCapture();
            if (turnLeft)
            {
                transform.RotateAround(Vector3.down, Time.deltaTime * rotationspeed);
            }

            if (turnRight)
            {
                transform.RotateAround(Vector3.up, Time.deltaTime * rotationspeed);
            }

            if (shouldBreak && breakTimer > 0)
                shouldMove = false;
            else
                shouldMove = true;

            if (shouldMove)
                movespeed = Mathf.SmoothDamp(movespeed, maxSpeed, ref acceleration, jerkTime);
            else
                movespeed = Mathf.SmoothDamp(movespeed, 0, ref acceleration, jerkTime);

            Vector3 forwardFlat = transform.forward.normalized;
            forwardFlat.y = 0;

            transform.position += forwardFlat * movespeed * Time.deltaTime;
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
            // if (Input.GetKeyDown(KeyCode.UpArrow)) shouldMove = true;
            // if (Input.GetKeyUp(KeyCode.UpArrow)) shouldMove = false;
            if (Input.GetKeyDown(KeyCode.W)) shouldMove = true;
            if (Input.GetKeyUp(KeyCode.W)) shouldMove = false;
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                shouldBreak = true;
                // shouldMove = false;
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                breakTimer -= Time.deltaTime;
            }
            else
            {
                breakTimer += Time.deltaTime;
            }

            breakTimer = Mathf.Clamp(breakTimer, 0, maxBreakTimer);

            if (Input.GetKeyUp(KeyCode.DownArrow)) shouldBreak = false;
        }
    }
}