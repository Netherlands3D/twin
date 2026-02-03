using Netherlands3D.Twin.Layers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Scenario : MonoBehaviour
{
    public LayerData Layer => layer;
    public Toggle Toggle => toggle == null ? GetComponent<Toggle>() : toggle;
    
    private Toggle toggle;
    private LayerData layer;
    
    private void Start()
    {
        toggle = GetComponent<Toggle>();
        toggle.isOn = false;
        
        // Auto resize the toggle based on text width
        var autoSize = toggle.GetComponent<AutoSizeTMPWidth>();
        if (autoSize != null)
        {
            autoSize.ResizeNow();
        }
    }

    public void SetLayer(LayerData layer)
    {
        this.layer = layer;
    }

    public void SetLabel(string name)
    {
        // Use the exact folder name as label (minus the 'Scenario:' prefix if you want)
        string label = name;
        int idx = label.IndexOf(':');
        if (idx >= 0 && idx + 1 < label.Length)
            label = label[(idx + 1)..].Trim(); // keep this if you want shorter labels
        // If you want the full name including "Scenario: ", just comment the 2 lines above
        // and use: string label = folder.Name;

        // TMP label first
        TMP_Text tmpText = gameObject.GetComponentInChildren<TMP_Text>();
        if (tmpText)
        {
            tmpText.text = label;
        }
        else
        {
            // Fallback to legacy Text if needed
            var text = toggle.GetComponentInChildren<Text>();
            if (text)
                text.text = label;
        }
    }
}