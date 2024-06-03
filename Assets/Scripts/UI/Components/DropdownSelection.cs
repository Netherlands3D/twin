using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Netherlands3D.Twin
{
    public class DropdownSelection : MonoBehaviour
    {
        public GameObject[] DropdownItems;
        public TMP_Dropdown Dropdownmenu;
        public int ItemNumber = 0;

       public void DropdownSelectItem()
        {
            ItemNumber = Dropdownmenu.value;
           if(DropdownItems.Length >= ItemNumber)
            {
                foreach (GameObject GameObject in DropdownItems)
                {
                    GameObject.SetActive(false);
                }
                DropdownItems[ItemNumber].SetActive(true);
                
            }
            
        }
    }
}
