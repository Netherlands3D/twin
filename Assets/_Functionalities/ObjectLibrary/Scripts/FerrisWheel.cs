using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectLibrary
{
    public class FerrisWheel : MonoBehaviour
    {
        [SerializeField] private GameObject cartTemplate;
        [SerializeField] private Transform wheel;
        [SerializeField] private float rotationSpeed = 1.0f;
        [SerializeField] private int cartCount = 32; //Carts in the wheel


        [ContextMenu("Respawn carts")]
        void SpawnCarts()
        {
            //Spawn carts on the wheel using same distance from wheel center as carttemplate on Y axis
            for (int i = 0; i < cartCount; i++)
            {
                GameObject cart = Instantiate(cartTemplate, cartTemplate.transform.position, Quaternion.identity);
                cart.transform.SetParent(wheel);
                cart.transform.localScale = cartTemplate.transform.localScale;

                cart.transform.RotateAround(wheel.position, Vector3.back, 360.0f / cartCount * i);        
            }

            PointCartsDown();
        }

        private void Update() {
            wheel.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

            PointCartsDown();
        }

        private void PointCartsDown()
        {
            foreach(Transform cart in wheel)
            {
                cart.transform.rotation = Quaternion.LookRotation(Vector3.up,wheel.up);
            }
        }
    }
}
