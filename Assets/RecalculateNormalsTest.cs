using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class RecalculateNormalsTest : MonoBehaviour
    {
        private void OnTransformChildrenChanged()
        {
            foreach (Transform child in transform)
            {
                var mf = child.GetComponent<MeshFilter>();
                var mesh = mf.mesh;
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                print("recalculating normals and bounds of: " + child.name);
            }
        }
    }
}
