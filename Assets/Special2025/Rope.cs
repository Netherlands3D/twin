using System;
using UnityEngine;

namespace Netherlands3D
{
    public class Rope : MonoBehaviour
    {
        [SerializeField] private Transform start, end, segmentContainer;
        [SerializeField] private GameObject segmentPrefab;
        [SerializeField] private int segmentCount = 10;
        [SerializeField] private float totalLength;

        private Transform[] joints = Array.Empty<Transform>();

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

        private void Start()
        {
            GenerateSegments();
        }

        private void GenerateSegments()
        {
            joints = new Transform[segmentCount-1];
            var previousTransform = start;
            JoinSegment(previousTransform, null, true); //start
            var dir = (end.position - start.position);

            for (int i = 0; i < segmentCount-1; i++)
            {
                var pos = previousTransform.position + (dir / segmentCount);
                var segment = Instantiate(segmentPrefab, pos, Quaternion.identity, segmentContainer);
                joints[i] = segment.transform;

                JoinSegment(segment.transform, previousTransform, false);

                previousTransform = segment.transform;
            }

            JoinSegment(end, previousTransform, false, true); //end
        }

        private void JoinSegment(Transform currentJoint, Transform connectedJoint, bool isKinematic = false, bool isClosed = false)
        {
            var rb = currentJoint.GetComponent<Rigidbody>();
            rb.isKinematic = isKinematic;
            rb.mass = totalWeight / segmentCount;
            rb.linearDamping = drag;
            rb.angularDamping = angularDrag;
            
            if (connectedJoint == null)
                return;

            var joint = currentJoint.GetComponent<ConfigurableJoint>();
            joint.connectedBody = connectedJoint.GetComponent<Rigidbody>();
            joint.autoConfigureConnectedAnchor = true;

            joint.connectedAnchor = isClosed ? Vector3.forward * 0.1f : Vector3.forward * (totalLength / segmentCount);

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