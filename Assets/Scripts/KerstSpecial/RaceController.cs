using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using Netherlands3D.Events;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Projects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin
{
    public class RaceController : MonoBehaviour
    {
        private FreeCamera freeCam;
        private float camPitch = 30f;
        private float camStartYaw = 202f;
        private float camHeight = 15f;
        private float playerDistToCamera = 32f;
        private float camLerpSpeed = 3f;
        private float camFollowSpeed = 3f;
        private float camYawDelta = 0;
        public float rotationSpeed = 60f;
        public float playerSpeed = 10f;
        public float playerOffRoadSpeed = 2.5f;
        private float playerCurrentSpeed;
        private float playerTargetSpeed;
        private float slipFactor = 0.99f;
        public float jumpForce = 10;
        private float checkPointScale = 5;

        private Vector3 unityStartTarget;
        private Vector3 playerMoveVector = Vector3.zero;
        private Quaternion cameraStartRotation;
        private bool isReadyForStart = false;
        public static bool isReadyToMove = false;
        private Layer maaiveld;
        private Layer gebouwen;

        public GameObject PlayerPrefab;
        private GameObject player;
        private Rigidbody playerRigidBody;
        public Collider playerCollider;
        public Animator PlayerAnimator;
        public Animator WeatherAnimator;
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
        private GameObject finishObject;
        public GameObject scoreBoard;

        public List<AudioClip> skateSounds = new List<AudioClip>();
        private AudioSource skateSource;
     
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
            GenerateFinish();
            InitPlayer();

            nothingMeshCollider = FindObjectOfType<ClickNothingPlane>().gameObject.GetComponent<MeshCollider>();

            //StartAnimation();
            TeleportCameraToStart();

            //StartCoroutine(WaitSeconds(20, () =>
            //{
            //    ResetPlayer();
            //}));
        }

        private IEnumerator WaitSeconds(float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            action?.Invoke();
        }

        public void TeleportCameraToStart()
        {
            Coordinate nextCoord = new Coordinate(CoordinateSystem.WGS84, routeCoords[0].x, routeCoords[0].y, 0);
            //if (currentCheckpoint == null)
            //    currentCheckpoint = SpawnCheckpoint(nextCoord);
            unityStartTarget = nextCoord.ToUnity();
            unityStartTarget.y = camHeight;
            freeCam.transform.position = unityStartTarget;
        }

        public void StartAnimation()
        {
            StartCoroutine(StartAnimationLoop());
        }

        private IEnumerator StartAnimationLoop()
        {
            while(!isReadyForStart)
            {
                Coordinate nextCoord = new Coordinate(CoordinateSystem.WGS84, routeCoords[0].x, routeCoords[0].y, 0);
                //if (currentCheckpoint == null)
                //    currentCheckpoint = SpawnCheckpoint(nextCoord);
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

        private void OnFinish(Collider col, ZoneTrigger trigger)
        {
            if(col == player.GetComponent<Collider>())
            {
                Finish();
            }
        }

        [ContextMenu("Finish now")]
        public void Finish()
        {
            Finished.Invoke();
        }

        private void InitPlayer()
        {
            player = GameObject.Instantiate(PlayerPrefab);
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
            playerCurrentSpeed = playerSpeed;
            playerCollider = player.GetComponent<Collider>();
            PlayerAnimator = player.GetComponentInChildren<Animator>();
            WeatherAnimator = this.GetComponent<Animator>();
            WeatherAnimator.SetBool("Storm", false);
            PlayerAnimator.SetBool("OnIce", true);

            skateSource = player.AddComponent<AudioSource>();
            skateSource.loop = false;
        }

        void PlayRandomClip()
        {
            int index = UnityEngine.Random.Range(0, skateSounds.Count - 1);
            if (skateSource.isPlaying)
                return;

            skateSource.clip = skateSounds[index];
            skateSource.pitch = Mathf.Clamp(playerMoveVector.magnitude / 1000f, 1, 5);
            skateSource.Play();
        }

        public void ResetPlayer()
        {
            StempelTrigger[] triggers = FindObjectsOfType<StempelTrigger>();
            foreach (StempelTrigger trigger in triggers)
                trigger.IsCollected = false;

            stempelkaart stempelkaart = FindObjectOfType<stempelkaart>(true);
            stempelkaart.SetStampEnabled(1, false);
            stempelkaart.SetStampEnabled(2, false);
            stempelkaart.SetStampEnabled(3, false);
            stempelkaart.SetStampMarkerEnabled(false);

            isReadyToMove = false;
            isReadyForStart = false;
            StartAnimation();
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
                PlayerAnimator.SetTrigger("Jump");
                jumpTimer = jumpInterval;
            }
        }

        private void OnEnter(Collider other, ZoneTrigger zone)
        {
            //Debug.Log("ONENTER" + other);
            //Debug.Log("zonenaam" + zone);
            if (zone.name == "zonetrigger2")
            {
                WeatherAnimator.SetBool("Storm", true);
            }
        }

        private void OnExit(Collider other, ZoneTrigger zone)
        {
            //Debug.Log("ONEXIT:" + other);
            if (zone.name == "zonetrigger2")
            {
                WeatherAnimator.SetBool("Storm", false);
            }
        }

        private Vector3 floorPoint = Vector3.zero;
        private MeshCollider currentMeshCollider;
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
                            currentMeshCollider = hits[i].collider as MeshCollider;
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
                    PlayerAnimator.ResetTrigger("Jump");
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

        public void GivePenaltySpeedToPlayer(float penalty)
        {
            playerCurrentSpeed *= penalty;
        }

        private void Update()
        {

            //if (IsDebugOn && Keyboard.current[Key.End].wasPressedThisFrame) {
            //    Finish();
            //}
            if (IsDebugOn && Keyboard.current[Key.Home].wasPressedThisFrame)
            {
                playerSpeed = 200;
                playerOffRoadSpeed = 200;
            }
            //if (IsDebugOn && Keyboard.current[Key.PageUp].wasPressedThisFrame)
            //{
            //    player.GetComponent<AudioSource>().enabled = false;
            //}

            if (routeCoords == null || !isReadyForStart)
                return;

            CheckNextCoordinate();
            jumpTimer -= Time.deltaTime;
            playerCurrentSpeed = Mathf.Lerp(playerCurrentSpeed, playerTargetSpeed, Time.deltaTime);
            PlayerAnimator.SetFloat("Speed", (playerMoveVector.z));
            if (currentMeshCollider != null)
            {
                if (currentMeshCollider.gameObject.layer == 4)//is on ice
                { 
                playerTargetSpeed = playerSpeed;
                PlayerAnimator.SetBool("OnIce", true);
                }
                else
                {
                    playerTargetSpeed = playerOffRoadSpeed;
                    PlayerAnimator.SetBool("OnIce", false);
                }              
            }

            if(playerMoveVector.magnitude / 1000 > 0.5f)
            {
                PlayRandomClip();
            }

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
                //currentCheckpoint = SpawnCheckpoint(nextCoord);
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
        public UnityEvent Finished;
        [SerializeField] private bool IsDebugOn = false;

        public void MoveHorizontally(float amount)
        {
            rotationDelta -= amount * Time.deltaTime * rotationSpeed;            
            playerMoveVector += Vector3.forward * playerCurrentSpeed * Mathf.Clamp01(Mathf.Abs(amount));
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
                        if (t.childCount == 0)
                        {
                            //t.gameObject.AddComponent<MeshCollider>();
                            //t.gameObject.isStatic = true;
                            //Debug.Log("ADDED MESHCOLLIDER FOR" + tileName);

                            MeshFilter meshFilter = t.gameObject.GetComponent<MeshFilter>();
                            if (meshFilter == null || meshFilter.mesh == null)
                            {
                                Debug.LogError("No MeshFilter or mesh found on this GameObject.");
                                return;
                            }

                            Mesh originalMesh = meshFilter.mesh;

                            for (int i = 0; i < originalMesh.subMeshCount; i++)
                            {
                                // Get triangles for the current submesh
                                int[] triangles = originalMesh.GetTriangles(i);

                                // Estimate maximum vertex count (worst case: each triangle uses unique vertices)
                                int maxVertices = triangles.Length;
                                Vector3[] originalVertices = originalMesh.vertices;

                                // Arrays for new vertices and remapped triangle indices
                                Vector3[] subMeshVertices = new Vector3[maxVertices];
                                int[] newTriangles = new int[triangles.Length];

                                // Mapping from original vertex index to new submesh index
                                int[] vertexMapping = new int[originalVertices.Length];
                                for (int v = 0; v < vertexMapping.Length; v++) vertexMapping[v] = -1;

                                int currentVertexIndex = 0;
                                for (int j = 0; j < triangles.Length; j++)
                                {
                                    int originalVertexIndex = triangles[j];

                                    // If this vertex has not been mapped, add it
                                    if (vertexMapping[originalVertexIndex] == -1)
                                    {
                                        vertexMapping[originalVertexIndex] = currentVertexIndex;
                                        subMeshVertices[currentVertexIndex] = originalVertices[originalVertexIndex];
                                        currentVertexIndex++;
                                    }

                                    // Remap triangle index to the new vertex index
                                    newTriangles[j] = vertexMapping[originalVertexIndex];
                                }

                                // Resize the subMeshVertices array to fit only the used vertices
                                Array.Resize(ref subMeshVertices, currentVertexIndex);

                                // Create a new mesh for the submesh
                                Mesh subMesh = new Mesh
                                {
                                    vertices = subMeshVertices,
                                    triangles = newTriangles
                                };
                                subMesh.RecalculateNormals();

                                // Create a new GameObject for the submesh
                                GameObject subMeshObject = new GameObject($"SubMesh_{i}_Collider");
                                if(i == originalMesh.subMeshCount - 1)
                                {
                                    //IS WATER LAYER
                                    subMeshObject.layer = LayerMask.NameToLayer("Water");
                                }

                                subMeshObject.transform.SetParent(t.gameObject.transform);
                                subMeshObject.transform.localPosition = Vector3.zero;
                                subMeshObject.transform.localRotation = Quaternion.identity;
                                subMeshObject.transform.localScale = Vector3.one;

                                // Add MeshFilter and assign the submesh
                                MeshFilter subMeshFilter = subMeshObject.AddComponent<MeshFilter>();
                                subMeshFilter.mesh = subMesh;

                                // Optionally hide the submesh visually by not adding a MeshRenderer

                                // Add MeshCollider and assign the submesh
                                MeshCollider meshCollider = subMeshObject.AddComponent<MeshCollider>();
                                meshCollider.sharedMesh = subMesh;
                                meshCollider.convex = false; // Set to true if needed

                            }
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
                            //Debug.Log("ADDED MESHCOLLIDER FOR" + tileName);
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

        private void GenerateFinish()
        {
            finishObject = new GameObject("finish");
            ZoneTrigger finishTrigger = finishObject.AddComponent<ZoneTrigger>();
            finishTrigger.OnEnter += OnFinish;
            WorldTransform wt = finishObject.AddComponent<WorldTransform>();
            GameObjectWorldTransformShifter shifter = finishObject.AddComponent<GameObjectWorldTransformShifter>();
            wt.SetShifter(shifter);
            BoxCollider bc = finishObject.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            finishObject.transform.localScale = new Vector3(100, 100, 10);
            finishObject.transform.rotation = Quaternion.AngleAxis(90, Vector3.up);
            Coordinate nextCoord = new Coordinate(CoordinateSystem.WGS84, routeCoords[routeCoords.Count -1].x, routeCoords[routeCoords.Count -1].y, 0);
            Vector3 unityCoord = nextCoord.ToUnity();
            unityCoord.y = 3;
            finishObject.transform.position = unityCoord;
        }

        //private GameObject SpawnCheckpoint(Coordinate coord)
        //{            
        //    GameObject routeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //    routeObject.transform.localScale = Vector3.one * checkPointScale;
        //    WorldTransform wt = routeObject.AddComponent<WorldTransform>();
        //    GameObjectWorldTransformShifter shifter = routeObject.AddComponent<GameObjectWorldTransformShifter>();
        //    wt.SetShifter(shifter);
        //    Vector3 unityCoord = coord.ToUnity();
        //    unityCoord.y = 3;
        //    routeObject.transform.position = unityCoord;
        //    return routeObject;
        //}

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