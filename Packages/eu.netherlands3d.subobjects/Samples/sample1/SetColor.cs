using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Netherlands3D.subobject;

public class SetColor : MonoBehaviour
{
    public string id;
    public Color color;
    // Start is called before the first frame update
    void Start()
    {
        Dictionary<string, Color> colorset = new Dictionary<string, Color>();
        colorset.Add(id, color);
        GeometryColorizer.AddAndMergeCustomColorSet(1, colorset);
    }

   
}
