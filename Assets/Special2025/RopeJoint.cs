using System;
using UnityEngine;

namespace Netherlands3D
{
    public class RopeJoint : MonoBehaviour
    {
        [SerializeField] 
        private bool kinematicOnCollision = false;
        private Rigidbody rb;

        private void Awake()
        {
            rb =  GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision other)
        {
            if(kinematicOnCollision)
                rb.isKinematic = true;
        }
    }
}
