using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D
{
    public class RopeNew : MonoBehaviour
    {
        [Header("Linking")]
        public float searchRadius = 10f;
        public LayerMask ropeLayer;

        [Header("Rope")]
        public Transform segmentPrefab;
        public float segmentLength = 5f;
        public float sagAmount = 1f;

        class RopeConnection
        {
            public RopeNew target;
            public List<Transform> segments = new();
            public int lastCount = -1;
        }

        readonly List<RopeConnection> connections = new();

        void Start()
        {
            FindNearbyRopes();
        }

        void Update()
        {
            for (int i = 0; i < connections.Count; i++)
            {
                UpdateRope(connections[i]);
            }
        }

        void FindNearbyRopes()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius, ropeLayer);

            foreach (var hit in hits)
            {
                var rope = hit.GetComponent<RopeNew>();
                if (!rope || rope == this) continue;

                // Master election: only one side creates rope
                if (GetInstanceID() > rope.GetInstanceID()) continue;

                CreateConnection(rope);
            }
        }

        void CreateConnection(RopeNew target)
        {
            if (!segmentPrefab) return;

            var conn = new RopeConnection
            {
                target = target
            };
            connections.Add(conn);
        }

        void UpdateRope(RopeConnection conn)
        {
            if (!conn.target) return;

            Vector3 start = transform.position;
            Vector3 end = conn.target.transform.position;

            float distance = Vector3.Distance(start, end);
            int count = Mathf.Max(1, Mathf.FloorToInt(distance / segmentLength));

            if (count != conn.lastCount)
            {
                Rebuild(conn, count);
                conn.lastCount = count;
            }

            // Quadratic Bezier-style sag
            Vector3 control = (start + end) * 0.5f + Vector3.down * sagAmount;

            float totalDistance = Vector3.Distance(start, end);
            float halfSeg = segmentLength * 0.5f;

            for (int i = 0; i < conn.segments.Count; i++)
            {
                // position along rope, leaving half segment at each end
                float distAlong = halfSeg + i * segmentLength;
                float t = distAlong / totalDistance;

                Vector3 pos = QuadraticBezier(start, control, end, t);
                conn.segments[i].position = pos;

                // rotation
                Vector3 nextPos = (i < conn.segments.Count - 1)
                    ? QuadraticBezier(start, control, end, (halfSeg + (i + 1) * segmentLength) / totalDistance)
                    : pos + (pos - QuadraticBezier(start, control, end, (halfSeg + (i - 1) * segmentLength) / totalDistance));

                conn.segments[i].rotation = Quaternion.LookRotation((nextPos - pos).normalized, Vector3.up);
            }
        }

        Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1 - t;
            return u * u * p0 + 2 * u * t * p1 + t * t * p2;
        }

        void Rebuild(RopeConnection conn, int count)
        {
            foreach (var s in conn.segments)
                if (s != null) Destroy(s.gameObject);

            conn.segments.Clear();

            for (int i = 0; i < count; i++)
            {
                var seg = Instantiate(segmentPrefab, transform);
                Vector3 scale = Vector3.one * segmentLength;
                scale.z = segmentLength * 0.66f;
                scale.y = scale.z;
                seg.localScale = scale;
                conn.segments.Add(seg);
            }
        }

        public void Despawn()
        {
            foreach (var conn in connections)
            {
                foreach (var seg in conn.segments)
                    if (seg != null) Destroy(seg.gameObject);
                conn.segments.Clear();
            }

            connections.Clear();
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, searchRadius);
        }
#endif
    }
}
