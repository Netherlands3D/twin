using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using Netherlands3D.Events;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Projects;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
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
        public float rotationSpeed = 60f;
        public float playerSpeed = 10f;
        private float slipFactor = 0.99f;
        public float jumpForce = 10;
        private float checkPointScale = 5;

        private Vector3 unityStartTarget;
        private Vector3 playerMoveVector = Vector3.zero;
        private Quaternion cameraStartRotation;
        private bool isReadyForStart = false;
        private bool isReadyToMove = false;
        private Layer maaiveld;
        private Layer gebouwen;

        private GameObject player;
        private Rigidbody playerRigidBody;
        private OpticalRaycaster raycaster;

        private Vector2[] zoneCenters = new Vector2[4] {
            new Vector2(53.241309f, 5.857816f),
            new Vector2(53.232692f, 5.853782f),
            new Vector2(53.224009f, 5.850627f),
            new Vector2(53.217379f, 5.847194f)
        };

        [SerializeField] private FloatEvent horizontalInput;
        [SerializeField] private BoolEvent jumpInput;

        [SerializeField] private TextAsset routeFile;
        private List<Vector2> routeCoords = new List<Vector2>();
        private int currentCoordinateIndex = 0;

        private MeshCollider nothingMeshCollider;
        private GameObject currentCheckpoint;
        private GameObject[] zoneObjects = new GameObject[4];
     
        private void Start()
        {
            freeCam = FindObjectOfType<FreeCamera>();

            if (!Application.isMobilePlatform)
            {
                horizontalInput.AddListenerStarted(MoveHorizontally);
                jumpInput.AddListenerStarted(Jump);
            }

            GetCoordinatesForRoute();
            GenerateZones();
            InitPlayer();

            nothingMeshCollider = FindObjectOfType<ClickNothingPlane>().gameObject.GetComponent<MeshCollider>();

            StartAnimation();
        }

        private void StartAnimation()
        {
            StartCoroutine(StartAnimationLoop());
        }

        private IEnumerator StartAnimationLoop()
        {
            while(!isReadyForStart)
            {
                Coordinate nextCoord = new Coordinate(CoordinateSystem.WGS84, routeCoords[0].x, routeCoords[0].y, 0);
                if (currentCheckpoint == null)
                    currentCheckpoint = SpawnCheckpoint(nextCoord);
                unityStartTarget = nextCoord.ToUnity();
                unityStartTarget.y = camHeight;
                cameraStartRotation = GetCameraStartRotation();
                float distToTarget = Vector3.Distance(freeCam.transform.position, unityStartTarget);
                float angleToTarget = Quaternion.Angle(freeCam.transform.rotation, cameraStartRotation);
                if (distToTarget > 1 || angleToTarget > 1)
                {
                    freeCam.transform.position = Vector3.Lerp(freeCam.transform.position, unityStartTarget, Time.deltaTime * camLerpSpeed);
                    freeCam.transform.rotation = Quaternion.Slerp(freeCam.transform.rotation, cameraStartRotation, Time.deltaTime * camLerpSpeed);
                }
                else
                {                    
                    player.transform.position = freeCam.transform.position + freeCam.transform.forward * playerDistToCamera;
                    isReadyForStart = true;
                }
                yield return new WaitForEndOfFrame();
            }
        }

        private void OnFinish()
        {

        }

        private void InitPlayer()
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerRigidBody = player.AddComponent<Rigidbody>();
            WorldTransform wt = player.AddComponent<WorldTransform>();
            GameObjectWorldTransformShifter shifter = player.AddComponent<GameObjectWorldTransformShifter>();
            wt.SetShifter(shifter);
            playerRigidBody.useGravity = false;
            playerRigidBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            playerRigidBody.angularDrag = 0;
            playerRigidBody.angularVelocity = Vector3.zero;
            playerRigidBody.mass = 100;
            Physics.gravity = Vector3.down * 30;
           
        }

        private bool hasJumped = false;
        private float jumpTimer = 0;
        private float jumpInterval = 1f;
        private void Jump(bool jump)
        {
            if(!hasJumped && jump && isReadyToMove && jumpTimer < 0)
            {
                playerRigidBody.AddForce(Vector3.up * playerRigidBody.mass * jumpForce, ForceMode.Impulse);
                hasJumped = true;
                jumpTimer = jumpInterval;
            }
        }

        private void OnEnter(Collider other, ZoneTrigger zone)
        {
            Debug.Log("ONENTER" + other);
        }

        private void OnExit(Collider other, ZoneTrigger zone)
        {
            Debug.Log("ONEXIT:" + other);
        }

        private Vector3 floorPoint = Vector3.zero;
        private void FixedUpdate()
        {
            if (!isReadyToMove)
                return;

            //playerRigidBody.AddForce(player.transform.rotation * playerMoveVector * Time.fixedDeltaTime);
            Vector3 vec = player.transform.rotation * playerMoveVector * Time.fixedDeltaTime;
            playerRigidBody.velocity = new Vector3(vec.x, playerRigidBody.velocity.y, vec.z);
            if (raycaster == null)
            {
                raycaster = FindObjectOfType<OpticalRaycaster>();
            }
            else
            {
                //Vector2 screenPoint = Camera.main.WorldToScreenPoint(player.transform.position + Vector3.down); //bottom of player
                //Vector3 floorPoint = raycaster.GetWorldPointAtCameraScreenPoint(Camera.main, new Vector3(screenPoint.x, screenPoint.y, 0));               
                RaycastHit[] hits = new RaycastHit[8];
                Physics.RaycastNonAlloc(playerRigidBody.transform.position + Vector3.up * 10, Vector3.down, hits);
                int mcCount = 0;
                float lowestMc = float.MaxValue;
                for(int i = 0; i < hits.Length; i++)
                {
                    if(hits[i].collider == null) continue;

                    if (hits[i].collider is MeshCollider mc && hits[i].collider != nothingMeshCollider)
                    {
                        if (hits[i].point.y < lowestMc)
                        {
                            //if the next hitpoint is suddenly above the player and the difference is greater than 0.5f then skip this next hitpoint
                            if (hits[i].point.y > playerRigidBody.transform.position.y && Mathf.Abs(hits[i].point.y - playerRigidBody.transform.position.y) > 0.5f)
                                continue;

                            lowestMc = hits[i].point.y;
                            floorPoint.y = lowestMc;
                        }
                    }
                }
                float yDist = Mathf.Abs(floorPoint.y - player.transform.position.y);
                //floorPoint.y = -1; //ugly fix but better for now
                bool isGrounded = yDist < 1f;
                //Debug.Log("isgrounded:" + isGrounded);
                if(isGrounded)
                {
                    hasJumped = false;
                }

                if (playerRigidBody.position.y < floorPoint.y - 0.1f)
                {
                    playerRigidBody.transform.SetPositionAndRotation(new Vector3(playerRigidBody.position.x, floorPoint.y, playerRigidBody.position.z), player.transform.rotation);
                }
                //Debug.Log("FLOORY" + floorPoint.y);
                //playerRigidBody.transform.SetPositionAndRotation(Vector3.Lerp(player.transform.position, new Vector3(player.transform.position.x, floorPoint.y, player.transform.position.z), Time.fixedDeltaTime * 3), player.transform.rotation);
                playerRigidBody.angularVelocity = Vector3.zero;
            }
        }

        private void Update()
        {
            
            if (routeCoords == null || !isReadyForStart)
                return;

            CheckNextCoordinate();
            jumpTimer -= Time.deltaTime;

            camYawDelta = Mathf.Lerp(camYawDelta, rotationDelta, Time.deltaTime * rotationSpeed);
            if (Mathf.Abs(camYawDelta) > 0.02f)
            {
                freeCam.transform.RotateAround(player.transform.position, Vector3.up, camYawDelta);
                freeCam.transform.LookAt(player.transform);
            }
            rotationDelta *= 0.5f;

            float distToCam = Vector3.Distance(player.transform.position, freeCam.transform.position);
            float factor = Mathf.Max(0, distToCam - playerDistToCamera) / playerDistToCamera;
            freeCam.transform.position = Vector3.Slerp(freeCam.transform.position, new Vector3(player.transform.position.x, camHeight, player.transform.position.z), Mathf.Min(Time.deltaTime * camFollowSpeed * factor, 0.03f));

            Vector3 camForward = freeCam.transform.forward;
            camForward.y = 0;

            player.transform.forward = Vector3.Slerp(player.transform.forward, camForward, Time.deltaTime * rotationSpeed);
            playerMoveVector *= slipFactor;
            if (playerMoveVector.magnitude <= 0.02f)
                playerMoveVector = Vector3.zero;

            GetClosestTileAndUpdateCollider(player.transform.position, out bool isGrounded);
            isReadyToMove = isGrounded;
            playerRigidBody.useGravity = isGrounded;
        }

        private void CheckNextCoordinate()
        {
            if (currentCoordinateIndex >= routeCoords.Count)
            {
                //FINISH
                return;
            }

            Coordinate nextCoord = new Coordinate(CoordinateSystem.WGS84, routeCoords[currentCoordinateIndex].x, routeCoords[currentCoordinateIndex].y, 0);
            Vector3 unityCoord = nextCoord.ToUnity();
            unityCoord.y = player.transform.position.y;
            float distance = Vector3.Distance(player.transform.position, unityCoord);
            //Debug.Log("distance to next target" +  distance);
            if(distance < checkPointScale)
            {
                Destroy(currentCheckpoint);

                currentCoordinateIndex++;
                nextCoord = new Coordinate(CoordinateSystem.WGS84, routeCoords[currentCoordinateIndex].x, routeCoords[currentCoordinateIndex].y, 0);
                //Debug.Log("WE HIT THE COORD" + currentCoordinateIndex);
                currentCheckpoint = SpawnCheckpoint(nextCoord);
            }
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

        private float rotationDelta = 0;
        public void MoveHorizontally(float amount)
        {
            rotationDelta -= amount * Time.deltaTime * rotationSpeed;            
            playerMoveVector += Vector3.forward * playerSpeed * Mathf.Clamp01(Mathf.Abs(amount));
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
            if(maaiveld == null || gebouwen == null)
            {
                GetLayers();
            }

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

        private void GetCoordinatesForRoute()
        {
            if (routeFile != null)
            {
                routeCoords = ExtractLatLon(routeFile.text);      
                //foreach(Vector2 c in routeCoords)
                //{
                //    Coordinate nextCoord = new Coordinate(CoordinateSystem.WGS84, c.x, c.y, 0);
                //    SpawnCheckpoint(nextCoord);
                //}    
                
            }
            else
            {
                Debug.LogError("GPX file not assigned.");
            }
        }

        private void GenerateZones()
        {
            for (int i = 0; i < zoneCenters.Length; i++)
            {
                GameObject zoneObject = new GameObject("zonetrigger" + i.ToString());
                ZoneTrigger trigger = zoneObject.AddComponent<ZoneTrigger>();
                trigger.OnEnter += OnEnter;
                trigger.OnExit += OnExit;
                WorldTransform wt = zoneObject.AddComponent<WorldTransform>();
                GameObjectWorldTransformShifter shifter = zoneObject.AddComponent<GameObjectWorldTransformShifter>();
                wt.SetShifter(shifter);
                BoxCollider bc = zoneObject.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                zoneObject.transform.localScale = Vector3.one * 1000; //1km per zone
                Coordinate nextCoord = new Coordinate(CoordinateSystem.WGS84, zoneCenters[i].x, zoneCenters[i].y, 0);
                Vector3 unityCoord = nextCoord.ToUnity();
                unityCoord.y = 3;
                zoneObject.transform.position = unityCoord;
            }
        }

        private GameObject SpawnCheckpoint(Coordinate coord)
        {            
            GameObject routeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            routeObject.transform.localScale = Vector3.one * checkPointScale;
            WorldTransform wt = routeObject.AddComponent<WorldTransform>();
            GameObjectWorldTransformShifter shifter = routeObject.AddComponent<GameObjectWorldTransformShifter>();
            wt.SetShifter(shifter);
            Vector3 unityCoord = coord.ToUnity();
            unityCoord.y = 3;
            routeObject.transform.position = unityCoord;
            return routeObject;
        }

        //List<Vector2> ExtractLatLon(string gpxContent)
        //{
        //    List<Vector2> latLonList = new List<Vector2>();
        //    XmlDocument xmlDoc = new XmlDocument();
        //    xmlDoc.LoadXml(gpxContent);

        //    XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
        //    nsmgr.AddNamespace("gpx", "http://www.topografix.com/GPX/1/1");

        //    XmlNodeList trkptNodes = xmlDoc.SelectNodes("//gpx:trkpt", nsmgr);

        //    foreach (XmlNode trkpt in trkptNodes)
        //    {
        //        if (trkpt.Attributes["lat"] != null && trkpt.Attributes["lon"] != null)
        //        {
        //            float lat = float.Parse(trkpt.Attributes["lat"].Value);
        //            float lon = float.Parse(trkpt.Attributes["lon"].Value);
        //            latLonList.Add(new Vector2(lat, lon));
        //        }
        //    }

        //    return latLonList;
        //}

        private List<Vector2> ExtractLatLon(string filePath)
        {
            List<Vector2> coordinates = new List<Vector2>();

            // Load and parse XML
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(filePath);

            // Find all "trkpt" elements (track points)
            XmlNodeList trackPoints = xmlDoc.GetElementsByTagName("trkpt");

            foreach (XmlNode node in trackPoints)
            {
                if (node.Attributes != null)
                {
                    // Extract latitude and longitude
                    string lat = node.Attributes["lat"]?.InnerText;
                    string lon = node.Attributes["lon"]?.InnerText;

                    if (!string.IsNullOrEmpty(lat) && !string.IsNullOrEmpty(lon))
                    {
                        float latitude = float.Parse(lat);
                        float longitude = float.Parse(lon);
                        coordinates.Add(new Vector2(latitude, longitude));
                    }
                }
            }

            return coordinates;
        }
    }
}
