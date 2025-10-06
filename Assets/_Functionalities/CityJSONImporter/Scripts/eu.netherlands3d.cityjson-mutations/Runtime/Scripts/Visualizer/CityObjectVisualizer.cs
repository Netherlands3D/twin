using Netherlands3D.CityJson.Structure;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.CityJson.Visualisation
{
    [RequireComponent(typeof(CityObject))]
    public abstract class CityObjectVisualizer: MonoBehaviour
    {
        public UnityEvent<CityObjectVisualizer> cityObjectVisualized;
        protected CityObject cityObject;

        protected virtual void Awake()
        {
            cityObject = GetComponent<CityObject>();
        }
        protected abstract void Visualize();
    }
}
