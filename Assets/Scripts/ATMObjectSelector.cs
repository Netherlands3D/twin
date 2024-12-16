using System;
using System.Collections.Generic;
using GeoJSON.Net.Feature;
using GG.Extensions;
using Netherlands3D.SubObjects;
using UnityEngine;

namespace Netherlands3D.Twin.ObjectInformation
{
    public class ATMObjectSelector : MonoBehaviour, IObjectSelector
    {
        public bool HasObjectMapping => foundObject != null;
        public ATMAsset Object => foundObject;
        public string ObjectID => foundId;

        [SerializeField] private float hitDistance = 100000f;
        private ATMAsset foundObject;
        private string foundId;

        private RaycastHit[] raycastHits = new RaycastHit[8];

        public void Select(Feature feature)
        {
            Deselect();

            if (foundObject != null)
            {
                //in this case the object has multiple material, lets color them all to prevent artefacts
                MeshRenderer mr = foundObject.GetComponent<MeshRenderer>();
                Material[] materials = mr.materials;
                foreach (Material material in materials)
                {
                    material.SetColor("_BaseColor", Color.blue);
                }
                mr.materials = materials;

                //set the color buffer of the vertices to the selection key to render the thumbnail
                MeshFilter mf = foundObject.GetComponent<MeshFilter>();
                Color[] colors = new Color[mf.mesh.vertexCount];
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = new Color(1, 0, 0, 0);
                mf.mesh.colors = colors;
            }
        }

        public void Deselect()
        {           
            if (foundObject != null)
            {
                MeshRenderer mr = foundObject.GetComponent<MeshRenderer>();
                Material[] materials = mr.materials;
                foreach (Material material in materials)
                {
                    material.SetColor("_BaseColor", Color.white);
                }
                mr.materials = materials;

                //set the colorbuffer of the vertices back to normal
                MeshFilter mf = foundObject.GetComponent<MeshFilter>();
                Color[] colors = new Color[mf.mesh.vertexCount];
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = new Color(1, 1, 1, 1);
                mf.mesh.colors = colors;
            }
        }

        private RaycastHit[] hits = new RaycastHit[8];
        public string FindSubObject(Ray ray, out RaycastHit hit)
        {
            foundObject = null;
            for (int i = 0; i < hits.Length; i++)
                hits[i] = new RaycastHit();

            hit = new RaycastHit();
            //because the opticalraycaster bug is still present the nothingplane gets selected so lets make an array and find the closest hit
            // use a nonalloc to prevent memory allocations
            int cnt = Physics.RaycastNonAlloc(ray, hits, hitDistance); 
            if (cnt == 0) return null;            

            float closest = float.MaxValue;
            for(int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider != null)
                {
                    var objectMapping = hits[i].collider.gameObject.GetComponent<ATMAsset>();
                    if (!objectMapping) continue;

                    float dist = Vector3.Distance(hits[i].point, ray.origin);
                    if (dist < closest)
                    {
                        closest = dist;
                        foundObject = objectMapping;
                        hit = hits[i];
                    }
                }
            }
            if (foundObject != null)
            {
                return foundObject.adamLink;
            }
            return null;
        }
    }
}