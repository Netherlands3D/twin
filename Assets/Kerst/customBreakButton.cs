using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class BreakButton : Button
    {
        public flyingCamera camerascript;

        public void Start()
        {
            camerascript = Camera.main.GetComponent<flyingCamera>();
        }
        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            camerascript.shouldBreak = true;
            //show text
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            camerascript.shouldBreak = false;
            //hide text
        }
    }
}