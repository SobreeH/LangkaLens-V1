using System;
using UnityEngine;

namespace LangkaLens.MR
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public class MRMeshPlayableArea : MonoBehaviour
    {
        public enum AreaState { Unconfigured, Aligning, TrackingUnavailable, Outside, NearBoundary, Inside }
        public enum FootprintMode { ScanInterior, RectangularHallways, MeshFloors }

        [Header("Walking surfaces from imported meshes")]
        [Tooltip("The scaled Parent transforms of the imported FLOOR planes. Their union defines walking space; the central hole stays empty.")]
        public Transform[] floorPlanes = Array.Empty<Transform>();
        private MRFloorFootprint _floorFootprint;
        public bool UsesMeshFloors => footprintMode == FootprintMode.MeshFloors;
        public MRFloorFootprint FloorFootprint
        {
            get
            {
                if (_floorFootprint == null && floorPlanes != null && floorPlanes.Length > 0 &&
                    Array.TrueForAll(floorPlanes, p => p != null))
                    _floorFootprint = new MRFloorFootprint(transform, floorPlanes);
                return _floorFootprint;
            }
        }

        public void RebuildFloorFootprint() { _floorFootprint = null; }
        private void OnValidate() { RebuildFloorFootprint(); }

        [Header("Playable footprint")]
        public FootprintMode footprintMode;
        [Tooltip("Outer rectangle in PlayableBoundary local X/Z metres. Rect Y means Z. Provisional until measured on site.")]
        public Rect hallwayOuter = new Rect(-8.5f, 0.5f, 21, 24);
        [Tooltip("Excluded central ruin/enclosure in local X/Z metres. Must lie strictly inside the outer rectangle.")]
        public Rect ruinExclusion = new Rect(-5, 4.5f, 14, 16.5f);

        public bool UsesHallways => footprintMode == FootprintMode.RectangularHallways;
        public float PlayableSquareMetres => UsesMeshFloors ? (FloorFootprint?.SquareMetres ?? 0) : UsesHallways ? hallwayOuter.width * hallwayOuter.height - ruinExclusion.width * ruinExclusion.height : squareMetres;
        public Vector2[] OuterOutline => UsesMeshFloors ? Array.Empty<Vector2>() : UsesHallways ? RectangleCorners(hallwayOuter) : outline;
        public Vector2[] ExclusionOutline => UsesHallways ? RectangleCorners(ruinExclusion) : Array.Empty<Vector2>();

        [Header("Interior baked from the surrounding scan meshes")]
        public MeshFilter[] boundaryMeshes;
        [Tooltip("Entrance arrow target in local X/Z metres. In legacy scan mode this is also the bake seed.")]
        public Vector2 interiorSeed;
        [Tooltip("Height of the horizontal mesh cross-section, relative to this object.")]
        public float sliceHeight = 1.2f;
        [HideInInspector] public Vector2[] outline = Array.Empty<Vector2>();
        [HideInInspector] public Matrix4x4[] bakedMeshTransforms = Array.Empty<Matrix4x4>();
        [HideInInspector] public float squareMetres;

        [Header("Tracked visitor")]
        public Transform head;
        public MRMeshReferenceAlignment alignment;
        public OVRPassthroughLayer passthrough;
        [Min(0.05f)] public float stopDistance = 0.25f;
        [Min(0.1f)] public float warningDistance = 0.8f;
        [Min(0)] public float recoveryMargin = 0.1f;

        [Header("Development")]
        public bool developmentMode = false;

        public AreaState State { get; private set; } = AreaState.Unconfigured;
        public float DistanceToEdge { get; private set; }
        public bool CanPlay => developmentMode || (isActiveAndEnabled && (State == AreaState.Inside || State == AreaState.NearBoundary));
        public string ConfigurationError { get; private set; }
        private bool _configured;

        private void Start()
        {
            _configured = ValidateConfiguration(out string error);
            ConfigurationError = error;
        }

        public bool ValidateConfiguration(out string error)
        {
            error = null;
            if (head == null || alignment == null || passthrough == null)
                error = "Assign the head, entrance alignment and passthrough references.";
            else if (Vector3.Distance(transform.lossyScale, Vector3.one) > 0.001f ||
                     !Finite(stopDistance) || !Finite(warningDistance) || !Finite(recoveryMargin) ||
                     stopDistance < 0.05f || warningDistance <= stopDistance || recoveryMargin < 0)
                error = "Use metre scale (1,1,1) and valid boundary distances.";
            else if (UsesMeshFloors)
            {
                if (floorPlanes == null || floorPlanes.Length == 0 ||
                    !Array.TrueForAll(floorPlanes, p => p != null && p.IsChildOf(transform) && p.lossyScale.sqrMagnitude > 0))
                    error = "Assign the floor planes from the imported site meshes.";
                else if (FloorFootprint == null || FloorFootprint.Edges.Length < 6 || FloorFootprint.SquareMetres <= 0)
                    error = "The imported floor planes do not define a walking area.";
            }
            else if (UsesHallways)
            {
                if (!ValidHallways(hallwayOuter, ruinExclusion))
                    error = "Use finite, positive rectangles with the ruin strictly inside the outer boundary.";
                else if (Mathf.Min(ruinExclusion.xMin - hallwayOuter.xMin, hallwayOuter.xMax - ruinExclusion.xMax,
                             ruinExclusion.yMin - hallwayOuter.yMin, hallwayOuter.yMax - ruinExclusion.yMax) <= 2 * (stopDistance + recoveryMargin))
                    error = "Every hallway must be wider than 2 x (stop distance + recovery margin).";
            }
            else if (outline == null || outline.Length < 3 || squareMetres <= 0)
                error = "Bake the mesh interior using the Inspector button.";
            else if (boundaryMeshes == null || bakedMeshTransforms == null || boundaryMeshes.Length != bakedMeshTransforms.Length)
                error = "The mesh list changed. Rebuild the interior outline.";
            else
            {
                for (int i = 0; i < boundaryMeshes.Length && error == null; i++)
                {
                    if (boundaryMeshes[i] == null) { error = "A boundary mesh is missing."; break; }
                    Matrix4x4 current = transform.worldToLocalMatrix * boundaryMeshes[i].transform.localToWorldMatrix;
                    for (int j = 0; j < 16; j++)
                        if (Mathf.Abs(current[j] - bakedMeshTransforms[i][j]) > 0.001f)
                        { error = "A scan section moved. Rebuild the interior outline."; break; }
                }
            }
            return error == null;
        }

        private void LateUpdate()
        {
            if (developmentMode)
            {
                State = AreaState.Inside;
                return;
            }
            
            if (!_configured) { State = AreaState.Unconfigured; return; }
            if (!alignment.IsConfirmed) { State = AreaState.Aligning; return; }
            if (!alignment.IsTrackingValid || !passthrough.isActiveAndEnabled || passthrough.hidden ||
                passthrough.textureOpacity < 0.95f || OVRManager.instance == null ||
                !OVRManager.instance.isInsightPassthroughEnabled || !OVRManager.IsInsightPassthroughInitialized())
            { State = AreaState.TrackingUnavailable; return; }

            State = EvaluatePositionState(head.position, CanPlay);
        }

        public AreaState EvaluatePositionState(Vector3 position, bool wasPlaying)
        {
            bool contained = ContainsWorldPoint(position, out float distance);
            DistanceToEdge = distance;
            float stop = stopDistance + (wasPlaying ? 0 : recoveryMargin);
            return !contained || distance <= stop ? AreaState.Outside :
                distance <= warningDistance ? AreaState.NearBoundary : AreaState.Inside;
        }

        public bool ContainsWorldPoint(Vector3 worldPoint, out float distance)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            if (!Finite(local.y)) { distance = 0; return false; }
            if (UsesMeshFloors)
            {
                if (FloorFootprint == null) { distance = 0; return false; }
                return FloorFootprint.Contains(new Vector2(local.x, local.z), out distance);
            }
            if (UsesHallways)
                return ContainsHallwayPoint(hallwayOuter, ruinExclusion, new Vector2(local.x, local.z), out distance);
            return ContainsPoint(outline, new Vector2(local.x, local.z), out distance);
        }

        public static bool ValidHallways(Rect outer, Rect inner) => ValidRect(outer) && ValidRect(inner) &&
            inner.xMin > outer.xMin && inner.xMax < outer.xMax && inner.yMin > outer.yMin && inner.yMax < outer.yMax;

        private static bool ValidRect(Rect r) => Finite(r.xMin) && Finite(r.xMax) && Finite(r.yMin) && Finite(r.yMax) && r.width > 0 && r.height > 0;

        public static Vector2[] RectangleCorners(Rect r) => new[] {
            new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin),
            new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) };

        // One ring, so the joins between the four hallways never act as boundaries.
        public static bool ContainsHallwayPoint(Rect outer, Rect inner, Vector2 point, out float distance)
        {
            distance = 0;
            if (!ValidHallways(outer, inner) || !Finite(point.x) || !Finite(point.y)) return false;
            bool insideOuter = RectContainsInclusive(outer, point);
            bool insideRuin = RectContainsInclusive(inner, point);
            distance = Mathf.Min(RectEdgeDistance(outer, point), RectEdgeDistance(inner, point));
            return insideOuter && !insideRuin;
        }

        private static bool RectContainsInclusive(Rect r, Vector2 p) => p.x >= r.xMin && p.x <= r.xMax && p.y >= r.yMin && p.y <= r.yMax;

        private static float RectEdgeDistance(Rect r, Vector2 p)
        {
            if (RectContainsInclusive(r, p))
                return Mathf.Min(Mathf.Min(p.x - r.xMin, r.xMax - p.x), Mathf.Min(p.y - r.yMin, r.yMax - p.y));
            return Vector2.Distance(p, new Vector2(Mathf.Clamp(p.x, r.xMin, r.xMax), Mathf.Clamp(p.y, r.yMin, r.yMax)));
        }

        public static bool ContainsPoint(Vector2[] polygon, Vector2 point, out float distance)
        {
            distance = float.PositiveInfinity;
            if (polygon == null || polygon.Length < 3 || !Finite(point.x) || !Finite(point.y)) return false;
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[j], b = polygon[i], edge = b - a;
                if (!Finite(a.x) || !Finite(a.y)) { distance = 0; return false; }
                float t = edge.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(point - a, edge) / edge.sqrMagnitude) : 0;
                distance = Mathf.Min(distance, Vector2.Distance(point, a + edge * t));
                if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside || distance < 0.0001f;
        }

        private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);

        private void OnDisable() { State = AreaState.Unconfigured; }

        private void OnDrawGizmosSelected()
        {
            if (UsesMeshFloors)
            {
                Gizmos.color = Color.cyan;
                Vector2[] edges = FloorFootprint?.Edges ?? Array.Empty<Vector2>();
                for (int i = 0; i < edges.Length; i += 2)
                    Gizmos.DrawLine(transform.TransformPoint(new Vector3(edges[i].x, 0.04f, edges[i].y)),
                        transform.TransformPoint(new Vector3(edges[i + 1].x, 0.04f, edges[i + 1].y)));
                return;
            }
            DrawOutlineGizmo(OuterOutline, Color.cyan);
            DrawOutlineGizmo(ExclusionOutline, new Color(1, 0.4f, 0.1f));
            if (UsesHallways) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.TransformPoint(new Vector3(interiorSeed.x, sliceHeight, interiorSeed.y)), 0.15f);
        }

        private void DrawOutlineGizmo(Vector2[] points, Color color)
        {
            if (points == null || points.Length < 3) return;
            Gizmos.color = color;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = points[i], b = points[(i + 1) % points.Length];
                Gizmos.DrawLine(transform.TransformPoint(new Vector3(a.x, 0.04f, a.y)), transform.TransformPoint(new Vector3(b.x, 0.04f, b.y)));
            }
        }
    }
}
