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
        public abstract Material[] Materials { get; }

        protected virtual void Awake()
        {
            cityObject = GetComponent<CityObject>();
        }
        
        protected virtual void OnEnable()
        {
            cityObject.CityObjectParsed.AddListener(Visualize);
        }

        protected virtual void OnDisable()
        {
            cityObject.CityObjectParsed.RemoveListener(Visualize);
        }
        
        protected abstract void Visualize();
        public abstract void SetFillColor(Color color);
        public abstract void SetLineColor(Color color);
    }
}
