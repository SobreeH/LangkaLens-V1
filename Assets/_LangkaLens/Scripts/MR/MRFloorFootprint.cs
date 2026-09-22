using System;
using System.Collections.Generic;
using UnityEngine;

namespace LangkaLens.MR
{
    // Uses the floor planes exported with the room meshes. Union edges exclude seams
    // between overlapping scan sections and retain the empty centre of the site.
    public sealed class MRFloorFootprint
    {
        private readonly List<Vector2[]> _floors = new List<Vector2[]>();
        public Vector2[] Edges { get; private set; } = Array.Empty<Vector2>();
        public float SquareMetres { get; private set; }
        private const float Epsilon = 0.0001f;

        public MRFloorFootprint(Transform space, Transform[] floorPlanes)
        {
            foreach (Transform floor in floorPlanes)
            {
                var polygon = new Vector2[4];
                for (int i = 0; i < 4; i++)
                {
                    // The imported Parent transform carries the floor width/length on X/Y.
                    Vector3 p = space.InverseTransformPoint(floor.TransformPoint(new Vector3(
                        i == 0 || i == 3 ? -0.5f : 0.5f, i < 2 ? -0.5f : 0.5f, 0)));
                    polygon[i] = new Vector2(p.x, p.z);
                }
                float signedArea = 0;
                for (int i = 0; i < 4; i++) signedArea += Cross(polygon[i], polygon[(i + 1) % 4]);
                if (signedArea < 0) Array.Reverse(polygon);
                _floors.Add(polygon);
            }
            BuildEdges();
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private bool Contains(Vector2 point)
        {
            foreach (var polygon in _floors)
                if (MRMeshPlayableArea.ContainsPoint(polygon, point, out _)) return true;
            return false;
        }

        public bool Contains(Vector2 point, out float clearance)
        {
            clearance = 0;
            if (float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsInfinity(point.x) || float.IsInfinity(point.y) || Edges.Length == 0)
                return false;
            clearance = float.PositiveInfinity;
            for (int i = 0; i < Edges.Length; i += 2)
            {
                Vector2 edge = Edges[i + 1] - Edges[i];
                float t = Mathf.Clamp01(Vector2.Dot(point - Edges[i], edge) / edge.sqrMagnitude);
                clearance = Mathf.Min(clearance, Vector2.Distance(point, Edges[i] + edge * t));
            }
            return Contains(point);
        }

        private void BuildEdges()
        {
            var edges = new List<Vector2>();
            foreach (var polygon in _floors)
            for (int i = 0; i < polygon.Length; i++)
            {
                Vector2 a = polygon[i], direction = polygon[(i + 1) % polygon.Length] - a;
                if (direction.sqrMagnitude < Epsilon * Epsilon) continue;
                var cuts = new List<float> { 0, 1 };
                foreach (var other in _floors)
                for (int j = 0; j < other.Length; j++)
                {
                    Vector2 b = other[j], edge = other[(j + 1) % other.Length] - b;
                    float divisor = Cross(direction, edge);
                    if (Mathf.Abs(divisor) > Epsilon)
                    {
                        float t = Cross(b - a, edge) / divisor;
                        float u = Cross(b - a, direction) / divisor;
                        if (t > 0 && t < 1 && u >= 0 && u <= 1) cuts.Add(t);
                    }
                    else if (Mathf.Abs(Cross(b - a, direction)) < Epsilon)
                    {
                        cuts.Add(Mathf.Clamp01(Vector2.Dot(b - a, direction) / direction.sqrMagnitude));
                        cuts.Add(Mathf.Clamp01(Vector2.Dot(b + edge - a, direction) / direction.sqrMagnitude));
                    }
                }
                cuts.Sort();
                Vector2 outward = new Vector2(direction.y, -direction.x).normalized * 0.001f;
                for (int j = 1; j < cuts.Count; j++)
                {
                    Vector2 start = a + direction * cuts[j - 1], end = a + direction * cuts[j];
                    if (Vector2.Distance(start, end) < Epsilon || Contains((start + end) * 0.5f + outward)) continue;
                    bool duplicate = false;
                    for (int k = 0; k < edges.Count; k += 2)
                        if ((Vector2.Distance(start, edges[k]) < Epsilon && Vector2.Distance(end, edges[k + 1]) < Epsilon) ||
                            (Vector2.Distance(end, edges[k]) < Epsilon && Vector2.Distance(start, edges[k + 1]) < Epsilon))
                        { duplicate = true; break; }
                    if (duplicate) continue;
                    edges.Add(start);
                    edges.Add(end);
                    SquareMetres += Cross(start, end) * 0.5f;
                }
            }
            Edges = edges.ToArray();
        }
    }
}
