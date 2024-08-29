using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_FloatToTextValue : MonoBehaviour
{
    private Text text;
    private InputField inputField;
    private TMP_Text tmp_text;
    private TMP_InputField tmp_inputField;

    [SerializeField]
    private bool interpolate = false;
    [SerializeField]
    private bool roundToInt = false;
    [SerializeField]
    private float minLerp = 0;
    [SerializeField]
    private float maxLerp = 100;

    private void Awake()
    {
        text = GetComponent<Text>();
        inputField = GetComponent<InputField>();
        tmp_text = GetComponent<TMP_Text>();
        tmp_inputField = GetComponent<TMP_InputField>();
    }

    public void SetFloatText(float value)
    {
        if (interpolate)
        {
            value = Mathf.Lerp(minLerp, maxLerp, value);
        }

        if(roundToInt)
        {
            value = Mathf.RoundToInt(value);
        }

        if (text) 
            text.text = value.ToString();
        if (inputField)
            inputField.text = value.ToString();
        if (tmp_text)
            tmp_text.text = value.ToString();
        if (tmp_inputField)
            tmp_inputField.text = value.ToString();
    }
}
