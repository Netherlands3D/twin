using System;
using System.Collections;
using System.Collections.Generic;
using NetTopologySuite.Triangulate;
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
        private Transform[] ropeMeshes;
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
            if (ropeMeshes == null)
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
                segment.name = "joint + " + i.ToString();

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

            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Limited; //prevent too much twisting

            var limit = new SoftJointLimit();
            limit.limit = 0;
            joint.angularZLimit = limit;

            var jointDrive = new JointDrive();
            jointDrive.positionDamper = 0;
            jointDrive.positionSpring = 0;
            joint.angularXDrive = jointDrive;
            joint.angularYZDrive = jointDrive;
        }

        private void GenerateMeshes()
        {
            ropeMeshes = new Transform[joints.Length - 1];

            for (int i = 0; i < ropeMeshes.Length; i++)
            {
                var mesh = Instantiate(
                    segmentPrefab,
                    jointContainer
                );

                ropeMeshes[i] = mesh.transform;
            }
        }

        private void UpdateMeshes()
        {
            for (int i = 0; i < ropeMeshes.Length; i++)
            {
                UpdateSegmentMesh(
                    ropeMeshes[i],
                    joints[i].position,
                    joints[i+1].position
                );
            }
        }

        private void UpdateSegmentMesh(
            Transform mesh,
            Vector3 a,
            Vector3 b
        )
        {
            Vector3 dir = b - a;
            float length = dir.magnitude;

            // Position in the middle
            mesh.position = a + dir * 0.5f;

            // Rotate to face B
            mesh.rotation = Quaternion.LookRotation(dir);

            // Scale along Z to match distance
            mesh.localScale = new Vector3(
                mesh.localScale.x,
                mesh.localScale.y,
                length
            );
        }
    }
}