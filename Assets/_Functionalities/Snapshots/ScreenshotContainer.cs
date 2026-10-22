using UnityEngine;

namespace Netherlands3D
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/ScreenshotContainer", order = 0)]
    [System.Serializable]
    public class ScreenshotContainer : ScriptableObject
    {
        public Sprite[] screenshots;
    }
}