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

        private void Awake()
        {
            DropdownSelectItem(Dropdownmenu.value);

            //Listen to change in the dropdown menu
            Dropdownmenu.onValueChanged.AddListener(DropdownSelectItem);
        }

        public void DropdownSelectItem(int changedToItem = 0)
        {
            ItemNumber = Dropdownmenu.value;
            if (DropdownItems.Length >= ItemNumber)
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
