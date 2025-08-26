using Netherlands3D.CityJson.Structure;
using Netherlands3D.Events;
using UnityEngine;

[RequireComponent(typeof(CityJSON))]
public class TestCityJSONInput : MonoBehaviour
{
    [SerializeField]
    private TextAsset testJson;
    [SerializeField]
    private StringEvent cityJSONReceived;

    protected void Start()
    {
        print(testJson.text);
        cityJSONReceived.InvokeStarted(testJson.text);
    }
}
