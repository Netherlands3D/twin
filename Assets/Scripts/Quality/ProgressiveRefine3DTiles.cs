using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Tiles3D;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ProgressiveRefine : MonoBehaviour
    {
        private Read3DTileset tilesetReader;
        private Matrix4x4 cameraMatrixLastFrame;

        [SerializeField] private int idleMaximumScreenSpaceError = 5;
        [SerializeField] private int movingMaximumScreenSpaceError = 40;
        [SerializeField] private int detailIncrementStepPerFrame = 5;

        void Awake()
        {
            tilesetReader = GetComponent<Read3DTileset>();
        }

        void Update()
        {
            //Compare the current camera matrix with the last frame
            var cameraIsIdle = cameraMatrixLastFrame == Camera.main.cameraToWorldMatrix;

            if (cameraIsIdle)
            {
                if(tilesetReader.maximumScreenSpaceError > idleMaximumScreenSpaceError)
                {
                    tilesetReader.maximumScreenSpaceError -= detailIncrementStepPerFrame;
                }
            }
            else if(tilesetReader.maximumScreenSpaceError < movingMaximumScreenSpaceError)
            {
                tilesetReader.maximumScreenSpaceError += detailIncrementStepPerFrame;
            }

            cameraMatrixLastFrame = Camera.main.cameraToWorldMatrix;
        }
    }
}
