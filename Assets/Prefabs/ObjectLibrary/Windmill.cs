using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Netherlands3D.T3DPipeline;

public enum WindmillStatus
{
    Unknown,
    Active,
    Planned,
    Removed
}

public class Windmill : MonoBehaviour
{
    private const string AXIS_HEIGHT_KEY = "asHoogte";
    private const string ROTOR_DIAMETER_KEY = "rotorDiameter";
    private const string STATUS_KEY = "status";

    [field:SerializeField]
    public float RotorDiameter { get; set; } = 120f;
    [field:SerializeField]
    public float AxisHeight { get; private set; } = 120f;
    [field:SerializeField]
    public WindmillStatus Status { get; private set; }

    [SerializeField]
    private GameObject windmillBase;
    [SerializeField]
    private GameObject windmillAxis;
    private Vector3 axisBasePosition;
    [SerializeField]
    private GameObject windmillRotorConnection;
    [SerializeField]
    private GameObject windmillRotor;
    [SerializeField]
    private float rotationSpeed = 10f;

    private void Awake()
    {
        axisBasePosition = windmillAxis.transform.localPosition;
    }

    public void InitializeFromCityObject(CityObject cityObject)
    {
        AxisHeight = cityObject.Attributes.First(attribute => attribute.Key == AXIS_HEIGHT_KEY).Value;
        RotorDiameter = cityObject.Attributes.First(attribute => attribute.Key == ROTOR_DIAMETER_KEY).Value;
        Status = ParseStatus(cityObject.Attributes.First(attribute => attribute.Key == STATUS_KEY).Value);

        if (Status == WindmillStatus.Planned)
        {
            if (AxisHeight == 0 || RotorDiameter == 0)
            {
                print("Windmill " + cityObject.Id + " has no heigt or diameter, using fallback height and diameter");
            }
        }

        RecalculateSize();
    }

    public void RecalculateSize()
    {
        windmillBase.transform.localScale = new Vector3(AxisHeight / 10, AxisHeight / 10, AxisHeight);
        windmillAxis.transform.localPosition = new Vector3(axisBasePosition.x, AxisHeight, axisBasePosition.z);
        windmillAxis.transform.localScale = AxisHeight * 0.1f * Vector3.one;
        windmillRotor.transform.localScale = new Vector3(RotorDiameter / 2, RotorDiameter / 2, RotorDiameter / 2);
        windmillRotor.transform.position = windmillRotorConnection.transform.position;
    }

    private static WindmillStatus ParseStatus(string statusString)
    {
        if (statusString.ToLower() == "in bedrijf")
            return WindmillStatus.Active;
        if (statusString.ToLower() == "toekomstig")
            return WindmillStatus.Planned;
        if (statusString.ToLower() == "gesaneerd")
            return WindmillStatus.Removed;

        return WindmillStatus.Unknown;
    }

    private void Update()
    {
        windmillRotor.transform.Rotate(Vector3.up, Time.deltaTime * rotationSpeed, Space.Self);
    }

    public void SetAxisHeight(float newHeight)
    {
        AxisHeight = newHeight;
        RecalculateSize();
    }

    public void SetRotorDiameter(float newDiameter)
    {
        RotorDiameter = newDiameter;
        RecalculateSize();
    }

    public void SetStatus(WindmillStatus newStatus)
    {
        Status = newStatus;
        RecalculateSize();
    }
}
