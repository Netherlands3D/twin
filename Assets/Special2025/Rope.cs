using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Netherlands3D
{
    public class Rope : MonoBehaviour
    {
        [SerializeField] private Transform start, end, jointContainer;
        [SerializeField] private GameObject jointPrefab;
        [SerializeField] private GameObject segmentPrefab;
        [SerializeField] private int initialSegmentCount = 10;
        private List<SpawnLights> segments = new();
        private List<Rigidbody> joints = new();

        private float totalWeight = 10;
        private float drag = 1;
        private float angularDrag = 1;
        
        [SerializeField] private float maxSegmentLength = 0.3f;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            foreach (var joint in joints)
            {
                Gizmos.DrawSphere(joint.position, 0.1f);
            }
        }
#endif

        private IEnumerator Start()
        {
            yield return null; //wait a frame so the gun applies the force to the start segment
            GenerateInitialRope();
            //     GenerateJoints();
            //     GenerateMeshes();
            //     var startSegmentSpeed = start.GetComponent<Rigidbody>().linearVelocity;
            //     foreach (var rb in GetComponentsInChildren<Rigidbody>())
            //     {
            //         rb.linearVelocity = startSegmentSpeed * Random.Range(0.9f, 1.1f);
            //         rb.angularVelocity = Vector3.zero;
            //     }
        }

        private void Update()
        {
            if (joints.Count > 1 && Vector3.Distance(joints[^2].position, end.position) > maxSegmentLength)
            {
                AddJoint();
            }
            
            if (segments == null)
                return;

            UpdateMeshes();
        }

        private void GenerateInitialRope()
        {
            joints.Clear();
            segments.Clear();

            joints.Add(start.GetComponent<Rigidbody>());

            Transform previous = start;

            Vector3 dir = (end.position - start.position);

            for (int i = 1; i <= initialSegmentCount; i++)
            {
                Vector3 pos = start.position + dir * i / (initialSegmentCount + 1);
                var jointGO = Instantiate(jointPrefab, pos, Quaternion.identity, jointContainer);
                jointGO.name = "joint " + i;
                var rb = jointGO.GetComponent<Rigidbody>();
                rb.maxAngularVelocity = 25f;
                rb.maxLinearVelocity = 50f;
                joints.Add(rb);

                ConnectJoint(jointGO.transform, previous);

                previous = jointGO.transform;
            }

            ConnectJoint(end, previous, true);
            joints.Add(end.GetComponent<Rigidbody>());

            // Generate segments
            for (int i = 0; i < joints.Count - 1; i++)
            {
                var segGO = Instantiate(segmentPrefab, jointContainer);
                segments.Add(segGO.GetComponent<SpawnLights>());
            }
        }

        private void AddJoint()
        {
            Transform previous = joints[^2].transform;

            Vector3 dir = end.position - previous.position;
            Vector3 newPos = previous.position + dir.normalized * maxSegmentLength;

            var jointGO = Instantiate(jointPrefab, newPos, Quaternion.identity, jointContainer);
            jointGO.name = "joint " + (joints.Count - 1);
            var rb = jointGO.GetComponent<Rigidbody>();
            rb.maxAngularVelocity = 25f;
            rb.maxLinearVelocity = 50f;

            ConnectJoint(jointGO.transform, previous);
            ConnectJoint(end, jointGO.transform, true);

            // Update lists
            joints.Insert(joints.Count - 1, rb);

            var segGO = Instantiate(segmentPrefab, jointContainer);
            segments.Insert(segments.Count - 1, segGO.GetComponent<SpawnLights>());
        }

        private void ConnectJoint(Transform currentJoint, Transform connectedJoint, bool isClosed = false)
        {
            if (connectedJoint == null) return;

            var joint = currentJoint.GetComponent<ConfigurableJoint>();
            joint.connectedBody = connectedJoint.GetComponent<Rigidbody>();
            joint.autoConfigureConnectedAnchor = true;

            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;

            var linearLimit = new SoftJointLimit();
            linearLimit.limit = Vector3.Distance(start.position, end.position) / (joints.Count - 1);
            joint.linearLimit = linearLimit;

            var linearDrive = new JointDrive { positionSpring = 0f, positionDamper = 5f, maximumForce = Mathf.Infinity };
            joint.xDrive = linearDrive;
            joint.yDrive = linearDrive;
            joint.zDrive = linearDrive;

            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Limited;

            var limit = new SoftJointLimit { limit = 0 };
            joint.angularZLimit = limit;

            var jointDrive = new JointDrive { positionSpring = 0, positionDamper = 5 };
            joint.angularXDrive = jointDrive;
            joint.angularYZDrive = jointDrive;
        }

        private void UpdateMeshes()
        {
            for (int i = 0; i < segments.Count; i++)
            {
                UpdateSegmentMesh(segments[i], joints[i].position, joints[i + 1].position);
            }
        }

        private void UpdateSegmentMesh(SpawnLights segment, Vector3 a, Vector3 b)
        {
            Vector3 dir = b - a;
            float length = dir.magnitude / 2;

            segment.transform.position = a + dir * 0.5f;
            segment.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90, 0, 0);
            segment.cylinder.localScale = new Vector3(segment.cylinder.localScale.x, length, segment.cylinder.localScale.z);
        }
        // private void GenerateMeshes()
        // {
        //     segments = new SpawnLights[joints.Length - 1];
        //
        //     for (int i = 0; i < segments.Length; i++)
        //     {
        //         var go = Instantiate(
        //             segmentPrefab,
        //             jointContainer
        //         );
        //
        //         segments[i] = go.GetComponent<SpawnLights>();
        //     }
        // }
        //
        // private void UpdateMeshes()
        // {
        //     for (int i = 0; i < segments.Length; i++)
        //     {
        //         UpdateSegmentMesh(
        //             segments[i],
        //             joints[i].position,
        //             joints[i+1].position
        //         );
        //     }
        // }
        //
        // private void UpdateSegmentMesh(
        //     SpawnLights segment,
        //     Vector3 a,
        //     Vector3 b
        // )
        // {
        //     Vector3 dir = b - a;
        //     float length = dir.magnitude/2;
        //
        //     // Position in the middle
        //     segment.transform.position = a + dir * 0.5f;
        //
        //     // Rotate to face B
        //     segment.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90, 0, 0);
        //
        //     // Scale along Z to match distance
        //     segment.cylinder.localScale = new Vector3(
        //         segment.cylinder.localScale.x,
        //         length,
        //         segment.cylinder.localScale.z
        //     );
        // }
    }
}