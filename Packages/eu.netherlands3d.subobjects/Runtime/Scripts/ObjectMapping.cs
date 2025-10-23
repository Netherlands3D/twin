using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.SubObjects
{
    public class ObjectMapping : MonoBehaviour
    {
        private void Start()
        {
            Interaction.CheckIn(this);
        }

        private void OnDestroy()
        {
            Interaction.CheckOut(this);
        }
        public List<ObjectMappingItem> items;

        public string getObjectID(int triangleIndex)
        {
            Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
            int firstIndex = mesh.triangles[3 * triangleIndex];
            
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i].firstVertex<=firstIndex)
                {
                    return items[i].objectID;
                }
            }
            return null;
        }
    }
   
}
