using System;
using System.Collections;
using UnityEngine;

namespace Netherlands3D
{
    public class SpawnLights : MonoBehaviour
    {
        private const float GOLDEN_ANGLE = 137.50776405f;
        [SerializeField] private LightColor lightPrefab;
        public Transform cylinder;

        [SerializeField] private float lightDensity = 10f;
        [SerializeField] private float surfaceOffset = 0.02f;
        [SerializeField] private Material[] lightMaterials;
        
        private IEnumerator Start()
        {
            yield return null; //wait for the length to be set
            GenerateLights();
        }
        

        public void GenerateLights()
        {
            ClearChildren();

            if (!lightPrefab || lightDensity <= 0f)
                return;

            float length = cylinder.localScale.y;

            // int lightCount = Mathf.RoundToInt(length *1.2f* lightDensity);
            int lightCount = 3;
            // float spacing = 1f / lightDensity;

            float spacing = length / (lightCount - 1);
            float start = -length * 0.5f;

            for (int i = 0; i < lightCount; i++)
            {
                float y = start + i * spacing;
                float angle = i * GOLDEN_ANGLE * Mathf.Deg2Rad;

                // Unity cylinder radius is 0.5, scaled by X
                float radius = cylinder.localScale.x * 10;

                float x = Mathf.Cos(angle) * (radius + surfaceOffset);
                float z = Mathf.Sin(angle) * (radius + surfaceOffset);

                Vector3 localPos = new Vector3(x, z, y);
                Vector3 worldPos = cylinder.TransformPoint(localPos);

                var light = Instantiate(lightPrefab, worldPos, Quaternion.identity, transform);
                SetLightMaterial(i, light);
                
                Vector3 localNormal = new Vector3(x, 0f, z).normalized;
                Vector3 worldNormal = cylinder.TransformDirection(localNormal);

                light.transform.rotation = Quaternion.LookRotation(worldNormal, cylinder.up) * Quaternion.Euler(90, 0, 0);;
            }
        }

        private void SetLightMaterial(int i, LightColor lightColor)
        {
            var index = i%lightMaterials.Length;
            var mat = lightMaterials[index];
            lightColor.SetMaterial(mat);
        }

        private void ClearChildren()
        {
            foreach (Transform child in transform)
            {
                if (child == cylinder)
                    continue;
                Destroy(child.gameObject);
            }
        }
    }
}