using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    
    [CreateAssetMenu(menuName = "Create Character", fileName = "Character", order = 0)]
    public class Character : ScriptableObject
    {
        public Sprite avatar;

        public string name;
        [TextArea]
        public string description;
    }
}
