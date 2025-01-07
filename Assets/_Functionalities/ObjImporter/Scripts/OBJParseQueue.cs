using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    public class OBJParseQueue : MonoBehaviour
    {
        public static OBJParseQueue Instance;

        public static bool IsParsing = false; //todo: the obj parser currently mixes up meshes if multiple are parsed at once. As a quick fix, only parse one at a time.
        public static Queue<ObjSpawner> ParseQueue = new();
        public static Coroutine QueueCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
        
        public static void Enqueue(ObjSpawner spawner)
        {
            if (spawner == null) return;

            ParseQueue.Enqueue(spawner);

            // Start processing if not already active
            if (!IsParsing)
            {
                Instance.StartCoroutine(Instance.ProcessQueue());
            }
        }
        
        public static void Remove(ObjSpawner spawner)
        {
            if (ParseQueue.Contains(spawner))
            {
                // Rebuild the queue without the removed spawner
                var tempQueue = new Queue<ObjSpawner>();
                while (ParseQueue.Count > 0)
                {
                    var item = ParseQueue.Dequeue();
                    if (item != spawner)
                    {
                        tempQueue.Enqueue(item);
                    }
                }

                foreach (var item in tempQueue)
                {
                    ParseQueue.Enqueue(item);
                }
            }
        }
        
        private IEnumerator ProcessQueue()
        {
            IsParsing = true;

            while (ParseQueue.Count > 0)
            {
                ObjSpawner currentSpawner = ParseQueue.Dequeue();

                // Skip null or destroyed OBJSpawners
                if (currentSpawner == null)
                {
                    continue;
                }

                currentSpawner.StartImport();

                // Wait until the current spawner signals completion
                while (!currentSpawner.IsImportComplete)
                {
                    yield return null;
                }
            }

            IsParsing = false;
        }
    }
}