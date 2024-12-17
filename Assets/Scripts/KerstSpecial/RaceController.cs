using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using Netherlands3D.Events;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Projects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin
{
    public class RaceController : MonoBehaviour
    {
        private FreeCamera freeCam;
        private float camPitch = 30f;
        private float camStartYaw = 93.5f;
        private float camHeight = 15f;
        private float playerDistToCamera = 32f;
        private float camLerpSpeed = 3f;
        private float camFollowSpeed = 3f;
        private float camYawDelta = 0;
        private float rotationSpeed = 60f;
        private float playerSpeed = 10f;
        private float slipFactor = 0.99f;

        private Vector3 unityStartTarget;
        private Vector3 playerMoveVector = Vector3.zero;
        private Quaternion cameraStartRotation;
        private bool isReadyForStart = false;
        private Layer maaiveld;
        private Layer gebouwen;

        private GameObject player;
        private Rigidbody playerRigidBody;
        private OpticalRaycaster raycaster;

        [SerializeField] private FloatEvent horizontalInput;
        [SerializeField] private FloatEvent verticalInput;
        [SerializeField] private FloatEvent upDownInput;

        //53.198472, 5.791865
        private Coordinate coord = new Coordinate(CoordinateSystem.WGS84, 53.198472d, 5.791865d, 0);

        private void Awake()
        {
            
        }

        private void Start()
        {
            freeCam = FindObjectOfType<FreeCamera>();
            cameraStartRotation = GetCameraStartRotation();

            horizontalInput.AddListenerStarted(MoveHorizontally);
            verticalInput.AddListenerStarted(MoveForwardBackwards);
            upDownInput.AddListenerStarted(MoveUpDown);

            player = GameObject.CreatePrimitive(PrimitiveType.Capsule); 
            playerRigidBody = player.AddComponent<Rigidbody>();
            playerRigidBody.useGravity = false;
            playerRigidBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            playerRigidBody.angularDrag = 0;
            playerRigidBody.angularVelocity = Vector3.zero;

            WorldTransform wt = player.AddComponent<WorldTransform>();
            GameObjectWorldTransformShifter shifter = player.AddComponent<GameObjectWorldTransformShifter>();
            wt.SetShifter(shifter);
            GetLayers();
        }

        private void FixedUpdate()
        {
            //playerRigidBody.AddForce(player.transform.rotation * playerMoveVector * Time.fixedDeltaTime);
            playerRigidBody.velocity = player.transform.rotation * playerMoveVector * Time.fixedDeltaTime;
            if (raycaster == null)
            {
                raycaster = FindObjectOfType<OpticalRaycaster>();
            }
            else
            {
                Vector2 screenPoint = Camera.main.WorldToScreenPoint(player.transform.position + Vector3.down); //bottom of player
                Vector3 floorPoint = raycaster.GetWorldPointAtCameraScreenPoint(Camera.main, new Vector3(screenPoint.x, screenPoint.y, 0));
                //float yDist = Mathf.Abs(floorPoint.y - player.transform.position.y);
                floorPoint.y = -1; //ugly fix but bettter for now
                playerRigidBody.transform.SetPositionAndRotation(Vector3.Slerp(player.transform.position, new Vector3(player.transform.position.x, floorPoint.y, player.transform.position.z), Time.fixedDeltaTime * 3), player.transform.rotation);
                playerRigidBody.angularVelocity = Vector3.zero;
            }
        }

        private void Update()
        {
            unityStartTarget = coord.ToUnity();
            unityStartTarget.y = camHeight;
            if ((Vector3.Distance(freeCam.transform.position, unityStartTarget) > 1 || Quaternion.Angle(freeCam.transform.rotation, cameraStartRotation) > 1) && !isReadyForStart)
            {
                freeCam.transform.position = Vector3.Lerp(freeCam.transform.position, unityStartTarget, Time.deltaTime * camLerpSpeed);
                freeCam.transform.rotation = Quaternion.Slerp(freeCam.transform.rotation, cameraStartRotation, Time.deltaTime * camLerpSpeed);
            }
            else
            {
                if (!isReadyForStart)
                {
                    player.transform.position = freeCam.transform.position + freeCam.transform.forward * playerDistToCamera;                    
                }
                isReadyForStart = true;
            }

            if(isReadyForStart)
            {
                if (Mathf.Abs(camYawDelta) > 0.02f)
                {
                    freeCam.transform.RotateAround(player.transform.position, Vector3.up, camYawDelta);
                    freeCam.transform.LookAt(player.transform);
                }               
                camYawDelta *= 0.5f; 

                float distToCam = Vector3.Distance(player.transform.position, freeCam.transform.position);
                float factor = Mathf.Max(0, distToCam - playerDistToCamera) / playerDistToCamera;
                freeCam.transform.position = Vector3.Slerp(freeCam.transform.position, new Vector3(player.transform.position.x, camHeight, player.transform.position.z), Mathf.Min(Time.deltaTime * camFollowSpeed * factor, 0.03f));
                
                Vector3 camForward = freeCam.transform.forward;
                camForward.y = 0;

                player.transform.forward = Vector3.Slerp(player.transform.forward, camForward, Time.deltaTime * rotationSpeed);
            }

            playerMoveVector *= slipFactor;
            if (playerMoveVector.magnitude <= 0.02f)
                playerMoveVector = Vector3.zero;

            

            GetClosestTileAndUpdateCollider(player.transform.position, out bool isGrounded);            
            playerRigidBody.useGravity = isGrounded;
        }

        private Quaternion GetCameraStartRotation()
        {
            Quaternion freeCamRotation = freeCam.transform.rotation;
            Vector3 euler = freeCam.transform.eulerAngles;
            euler.x = camPitch;
            euler.y = camStartYaw;
            euler.z = 0;
            freeCamRotation = Quaternion.Euler(euler);
            return freeCamRotation;
        }

        public void MoveHorizontally(float amount)
        {
            camYawDelta += amount * Time.deltaTime * rotationSpeed;
        }

        public void MoveForwardBackwards(float amount)
        {
            playerMoveVector += Vector3.forward * playerSpeed * Mathf.Clamp01(amount);
        }

        public void MoveUpDown(float amount)
        {
            
        }

        public void GetLayers()
        {
            CartesianTiles.TileHandler th = FindObjectOfType<CartesianTiles.TileHandler>();
            foreach(Layer layer in th.layers)
                if(layer != null)
                {
                    if (layer.gameObject.name.Contains("Maaiveld"))
                        maaiveld = layer;                   
                    if(layer.gameObject.name.Contains("Gebouwen"))
                        gebouwen = layer;
                }
        }

        public void GetClosestTileAndUpdateCollider(Vector3 position, out bool isGrounded)
        {
            Vector2Int tileKey = GetTileKeyFromUnityPosition(position, 1000);
            string tileName = tileKey.x.ToString() + "-" + tileKey.y.ToString();
            isGrounded = false;
            if (maaiveld != null)
            {
                foreach (Transform t in maaiveld.transform.GetComponentInChildren<Transform>())
                {
                    if(t.name == tileName)
                    {
                        isGrounded = true;
                        //yes very ugly but very easy
                        if (t.gameObject.GetComponent<Collider>() == null)
                        {
                            t.gameObject.AddComponent<MeshCollider>();
                            t.gameObject.isStatic = true;
                            Debug.Log("ADDED MESHCOLLIDER FOR" + tileName);
                        }
                    }                    
                }
            }
            if (gebouwen != null)
            {
                foreach (Transform t in gebouwen.transform.GetComponentInChildren<Transform>())
                {
                    if (t.name == tileName)
                    {
                        //yes very ugly but very easy
                        if (t.gameObject.GetComponent<Collider>() == null)
                        {
                            t.gameObject.AddComponent<MeshCollider>();
                            t.gameObject.isStatic = true;
                            Debug.Log("ADDED MESHCOLLIDER FOR" + tileName);
                        }

                    }
                }
            }
        }

        public Vector2Int GetTileKeyFromUnityPosition(Vector3 position, int tileSize)
        {
            var unityCoordinate = new Coordinate(
                CoordinateSystem.Unity,
                position.x,
                position.y,
                position.z
            );
            Coordinate coord = CoordinateConverter.ConvertTo(unityCoordinate, CoordinateSystem.RD);
            Vector2Int key = new Vector2Int(Mathf.RoundToInt(((float)coord.Points[0] - 0.5f * tileSize) / 1000) * 1000, Mathf.RoundToInt(((float)coord.Points[1] - 0.5f * tileSize) / 1000) * 1000);
            return key;
        }
    }
}
