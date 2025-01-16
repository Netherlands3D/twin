using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectInformation
{
    [CreateAssetMenu(menuName = "Netherlands3D/Data/BagIdsHiddenData", fileName = "BagIdsHidden", order = 0)]
    public class HiddenBagIds : ScriptableObject
    {         
        [SerializeField] public List<string> bagIds = new List<string>();
    }
}
