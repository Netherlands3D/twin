using System.Collections.Generic;
using System;
using UnityEngine;

namespace Netherlands3D.ObjImporter.General.GameObjectDataSet
{
    [Serializable]
    public class GameObjectDataSet
    {
        public string name;
        public List<MaterialData> materials = new List<MaterialData>();
        public List<GameObjectData> gameObjects = new List<GameObjectData>();
        public Vector3 Origin = Vector3.zero;
        public void Clear()
        {
            foreach (GameObjectData item in gameObjects)
            {
                item.Clear();
            }
            gameObjects.Clear();
            foreach (MaterialData item in materials)
            {
                item.Clear();
            }
            materials.Clear();
        }
    }

}