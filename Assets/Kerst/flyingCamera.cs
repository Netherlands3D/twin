using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class flyingCamera : MonoBehaviour
    {

        public float rotationspeed=1;
        public float movespeed=1000;


        public bool shouldMove = false;
        public bool turnLeft = false;
        public bool turnRight = false;

        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            keyCapture();
            if (turnLeft)
            {
                transform.RotateAround(Vector3.down, Time.deltaTime  * rotationspeed);
                
            }
            if (turnRight)
            {
                transform.RotateAround(Vector3.up, Time.deltaTime  * rotationspeed);
                
            }
            if (shouldMove)
            {
                Vector3 forwardFlat = transform.forward;
                forwardFlat.y = 0;
                transform.position += forwardFlat * movespeed * Time.deltaTime;
            }
        }

        void keyCapture()
        {
            //turn left
            if(Input.GetKeyDown(KeyCode.LeftArrow)) turnLeft = true;
            
            if (Input.GetKeyUp(KeyCode.LeftArrow)) turnLeft = false;

            if (Input.GetKeyDown(KeyCode.Q)) turnLeft = true;
            if (Input.GetKeyUp(KeyCode.Q)) turnLeft = false;

            //turn right
            if (Input.GetKeyDown(KeyCode.RightArrow)) turnRight = true;
            if (Input.GetKeyUp(KeyCode.RightArrow)) turnRight = false;
            if (Input.GetKeyDown(KeyCode.E)) turnRight = true;
            if (Input.GetKeyUp(KeyCode.E)) turnRight = false;

            //move
            if (Input.GetKeyDown(KeyCode.UpArrow)) shouldMove = true;
            if (Input.GetKeyUp(KeyCode.UpArrow)) shouldMove = false;
            if (Input.GetKeyDown(KeyCode.W)) shouldMove = true;
            if (Input.GetKeyUp(KeyCode.W)) shouldMove = false;
        }
        
    }
}
