using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.SubObjects
{
    public class ObjectMapping : MonoBehaviour
    {
        public Dictionary<string, ObjectMappingItem> items = new();
        
        private void Start()
        {
            Interaction.CheckIn(this);
        }

        private void OnDestroy()
        {
            Interaction.CheckOut(this);
        }
    }
}
