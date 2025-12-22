using System;
using UnityEngine;

namespace Netherlands3D
{
    public class SplattableObject : MonoBehaviour
    {
        [SerializeField] private GameObject splatVisual;
        
        private void OnCollisionEnter(Collision col)
        {
            if (col.contactCount == 0)
                return;
            
            var contact = col.GetContact(0);
            
            
            CreateSplat(contact.point, contact.normal);
        }

        private void CreateSplat(Vector3 position, Vector3 normal)
        {
            var rot =  Quaternion.LookRotation(-normal, Vector3.up);
            Instantiate(splatVisual, position + (0.5f*normal), rot);
        }
    }
}
