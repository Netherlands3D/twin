using Netherlands3D.FirstPersonViewer.Events;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer
{
    public class FirstPersonViewerData : MonoBehaviour
    {
        [field:SerializeField] public Camera FPVCamera { private set; get; }
        [field:SerializeField] public MovementModusSwitcher ModusSwitcher { private set; get; } 
    }
}
