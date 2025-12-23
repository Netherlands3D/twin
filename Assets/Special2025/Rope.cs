using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Netherlands3D
{
    public class Rope : MonoBehaviour
    {
        [SerializeField] private Transform start, end, jointContainer;
        [SerializeField] private GameObject jointPrefab;
        [SerializeField] private GameObject segmentPrefab;
        [SerializeField] private int segmentCount = 10;
        private SpawnLights[] segments;
        private Rigidbody[] joints = Array.Empty<Rigidbody>();

        private float totalWeight = 10;
        private float drag = 1;
        private float angularDrag = 1;

        private void OnDrawGizmos()
        {
            foreach (var joint in joints)
            {
                Gizmos.DrawSphere(joint.position, 0.1f);
            }
        }

        private IEnumerator Start()
        {
            yield return null; //wait a frame so the gun applies the force to the start segment
            GenerateJoints();
            GenerateMeshes();
            var startSegmentSpeed = start.GetComponent<Rigidbody>().linearVelocity;
            foreach (var rb in GetComponentsInChildren<Rigidbody>())
            {
                rb.linearVelocity = startSegmentSpeed * Random.Range(0.9f, 1.1f);
                rb.angularVelocity = Vector3.zero;
            }
        }

        private void Update()
        {
            if (segments == null)
                return;

            UpdateMeshes();
        }

        private void GenerateJoints()
        {
            joints = new Rigidbody[segmentCount + 1];
            joints[0] = start.GetComponent<Rigidbody>();
            var previousTransform = start;
            ConnectJoint(previousTransform, null); //start
            var dir = (end.position - start.position);

            for (int i = 1; i < segmentCount; i++)
            {
                var pos = previousTransform.position + (dir / segmentCount);
                var segment = Instantiate(jointPrefab, pos, Quaternion.identity, jointContainer);
                segment.name = "joint " + i;

                var joint = segment.GetComponent<Rigidbody>();
                joint.maxAngularVelocity = 25f;
                joint.maxLinearVelocity = 50f;
                joints[i] = joint;

                ConnectJoint(segment.transform, previousTransform, false);

                previousTransform = segment.transform;
            }

            ConnectJoint(end, previousTransform, true); //end
            joints[^1] = end.GetComponent<Rigidbody>();
        }

        private void ConnectJoint(Transform currentJoint, Transform connectedJoint, bool isClosed = false)
        {
            if (connectedJoint == null)
                return;

            var joint = currentJoint.GetComponent<ConfigurableJoint>();
            joint.connectedBody = connectedJoint.GetComponent<Rigidbody>();
            joint.autoConfigureConnectedAnchor = true;

            // var totalLength = start - end
            // joint.connectedAnchor = isClosed ? Vector3.forward * 0.1f : Vector3.forward * (totalLength / segmentCount);

            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;
            
            var linearLimit = new SoftJointLimit();
            float totalLength = Vector3.Distance(start.position, end.position);
            linearLimit.limit = totalLength / segmentCount; // max stretch per segment
            joint.linearLimit = linearLimit;
            
            var linearDrive = new JointDrive
            {
                positionSpring = 0f,       // 0 = no extra force pulling
                positionDamper = 5f,       // absorbs energy
                maximumForce = Mathf.Infinity
            };

            joint.xDrive = linearDrive;
            joint.yDrive = linearDrive;
            joint.zDrive = linearDrive;

            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Limited; //prevent too much twisting

            var limit = new SoftJointLimit();
            limit.limit = 0;
            joint.angularZLimit = limit;

            var jointDrive = new JointDrive();
            jointDrive.positionDamper = 5f;
            jointDrive.positionSpring = 0;
            joint.angularXDrive = jointDrive;
            joint.angularYZDrive = jointDrive;
        }

        private void GenerateMeshes()
        {
            segments = new SpawnLights[joints.Length - 1];

            for (int i = 0; i < segments.Length; i++)
            {
                var go = Instantiate(
                    segmentPrefab,
                    jointContainer
                );

                segments[i] = go.GetComponent<SpawnLights>();
            }
        }

        private void UpdateMeshes()
        {
            for (int i = 0; i < segments.Length; i++)
            {
                UpdateSegmentMesh(
                    segments[i],
                    joints[i].position,
                    joints[i+1].position
                );
            }
        }

        private void UpdateSegmentMesh(
            SpawnLights segment,
            Vector3 a,
            Vector3 b
        )
        {
            Vector3 dir = b - a;
            float length = dir.magnitude/2;

            // Position in the middle
            segment.transform.position = a + dir * 0.5f;

            // Rotate to face B
            segment.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90, 0, 0);

            // Scale along Z to match distance
            segment.cylinder.localScale = new Vector3(
                segment.cylinder.localScale.x,
                length,
                segment.cylinder.localScale.z
            );
        }
    }
}