using System;
using UnityEngine;

namespace LangkaLens.MR
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public class MRMeshPlayableArea : MonoBehaviour
    {
        public enum AreaState { Unconfigured, Aligning, TrackingUnavailable, Outside, NearBoundary, Inside }

        [Header("Interior baked from the surrounding scan meshes")]
        public MeshFilter[] boundaryMeshes;
        [Tooltip("A point in the enclosed central space, in this object's local X/Z coordinates.")]
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
            else if (outline == null || outline.Length < 3 || squareMetres <= 0)
                error = "Bake the mesh interior using the Inspector button.";
            else if (Vector3.Distance(transform.lossyScale, Vector3.one) > 0.001f ||
                     stopDistance < 0.05f || warningDistance <= stopDistance || recoveryMargin < 0)
                error = "Use metre scale (1,1,1) and valid boundary distances.";
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

            bool contained = ContainsWorldPoint(head.position, out float distance);
            DistanceToEdge = distance;
            float stop = stopDistance + (CanPlay ? 0 : recoveryMargin);
            State = !contained || distance <= stop ? AreaState.Outside :
                distance <= warningDistance ? AreaState.NearBoundary : AreaState.Inside;
        }

        public bool ContainsWorldPoint(Vector3 worldPoint, out float distance)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            return ContainsPoint(outline, new Vector2(local.x, local.z), out distance);
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
            if (outline == null || outline.Length < 3) return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 a = outline[i], b = outline[(i + 1) % outline.Length];
                Gizmos.DrawLine(transform.TransformPoint(new Vector3(a.x, 0.04f, a.y)), transform.TransformPoint(new Vector3(b.x, 0.04f, b.y)));
            }
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.TransformPoint(new Vector3(interiorSeed.x, sliceHeight, interiorSeed.y)), 0.15f);
        }
    }
}
