using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Netherlands3D
{
    public class Rope : MonoBehaviour
    {
        [SerializeField] private Transform start, end, segmentContainer;
        [SerializeField] private GameObject segmentPrefab;
        [SerializeField] private int segmentCount = 10;
        // [SerializeField] private float totalLength;

        private Rigidbody[] joints = Array.Empty<Rigidbody>();

        private float totalWeight = 10;
        private float drag = 1;
        private float angularDrag = 1;

        private void OnDrawGizmos()
        {
            foreach (var segment in joints)
            {
                Gizmos.DrawSphere(segment.position, 0.1f);
            }
        }

        private IEnumerator Start()
        {
            yield return null; //wait a frame so the gun applies the force to the start segment
            GenerateSegments();
            var startSegmentSpeed = start.GetComponent<Rigidbody>().linearVelocity;
            foreach (var rb in GetComponentsInChildren<Rigidbody>())
            {
                rb.linearVelocity = startSegmentSpeed * Random.Range(0.9f, 1.1f);
                rb.angularVelocity = Vector3.zero;
            }
        }

        private void GenerateSegments()
        {
            joints = new Rigidbody[segmentCount-1];
            var previousTransform = start;
            JoinSegment(previousTransform, null); //start
            var dir = (end.position - start.position);

            for (int i = 0; i < segmentCount-1; i++)
            {
                var pos = previousTransform.position + (dir / segmentCount);
                var segment = Instantiate(segmentPrefab, pos, Quaternion.identity, segmentContainer);
                
                var joint =  segment.GetComponent<Rigidbody>();
                joint.maxAngularVelocity = 25f;
                joint.maxLinearVelocity = 50f;
                joints[i] = joint;

                JoinSegment(segment.transform, previousTransform, false);

                previousTransform = segment.transform;
            }

            JoinSegment(end, previousTransform, true); //end
        }

        private void JoinSegment(Transform currentJoint, Transform connectedJoint, bool isClosed = false)
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
    }
}