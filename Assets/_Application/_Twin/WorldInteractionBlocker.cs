using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin
{
    public class WorldInteractionBlocker : MonoBehaviour, IPointerClickHandler
    {
        private static WorldInteractionBlocker instance;
        private static HashSet<object> blockingObjects = new();
        public static UnityEvent ClickedOnBlocker = new();

        private void Awake()
        {
            if (instance)
            {
                Debug.LogError("A WorldInteractionBlocker Instance already exists, destroying it and replacing it", gameObject);
                Destroy(instance.gameObject);
            }

            instance = this;
            UpdateBlock();
        }

        public static void AddBlocker(object requestingObject)
        {
            blockingObjects.Add(requestingObject);
            instance.UpdateBlock();
        }
        
        public static void ReleaseBlocker(object requestingObject)
        {
            blockingObjects.Remove(requestingObject);
            instance.UpdateBlock();
        }

        private void UpdateBlock()
        {
            gameObject.SetActive(blockingObjects.Count > 0);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ClickedOnBlocker.Invoke();
        }
    }
}
