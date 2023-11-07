
using Netherlands3D.SubObjects;
using UnityEngine;

public class TestClickBagID : MonoBehaviour
{
    public Mesh mesh;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                var hitIndex = hit.triangleIndex;
                
                string id = hit.collider.gameObject.GetComponent<ObjectMapping>().getObjectID(hitIndex);
                Debug.Log($"<color=green>{id}");
            }
        }
    }
}
