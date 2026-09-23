using UnityEngine;

namespace LangkaLens.MR
{
    /// <summary>Upright, least-squares similarity fit. Points are paired by label, never reordered.</summary>
    public static class MRFoundationFit
    {
        public struct Solution
        {
            public Vector3 position;
            public Quaternion rotation;
            public float scale;
            public float maxError;
            public float rmsError;
            public Vector3 TransformPoint(Vector3 point) => position + rotation * (point * scale);
        }

        public static bool TrySolve(Vector3[] source, Vector3[] target, bool solveScale,
            float fixedScale, out Solution solution)
        {
            solution = default;
            if (source == null || target == null || source.Length != target.Length || source.Length < 3 ||
                !Finite(fixedScale) || fixedScale <= 0) return false;
            Vector3 a = Vector3.zero, b = Vector3.zero;
            for (int i = 0; i < source.Length; i++)
            {
                if (!Finite(source[i]) || !Finite(target[i])) return false;
                a += source[i]; b += target[i];
            }
            a /= source.Length; b /= source.Length;
            double dot = 0, cross = 0, energy = 0, xx = 0, zz = 0, xz = 0;
            double txx = 0, tzz = 0, txz = 0;
            for (int i = 0; i < source.Length; i++)
            {
                Vector3 u = source[i] - a, v = target[i] - b;
                dot += u.x * v.x + u.z * v.z;
                cross += u.z * v.x - u.x * v.z;
                xx += u.x * u.x; zz += u.z * u.z; xz += u.x * u.z;
                txx += v.x * v.x; tzz += v.z * v.z; txz += v.x * v.z;
                energy += u.x * u.x + u.z * u.z;
            }
            // Reject collapsed/nearly collinear point sets and reflected corner order.
            if (energy < 1e-8 || xx * zz - xz * xz < energy * energy * 0.001 ||
                txx * tzz - txz * txz < (txx + tzz) * (txx + tzz) * 0.001 || txx + tzz < 1e-6 ||
                SignedArea(source) * SignedArea(target) <= 0) return false;
            float scale = solveScale ? (float)(System.Math.Sqrt(dot * dot + cross * cross) / energy) : fixedScale;
            if (!Finite(scale) || scale < 0.001f || scale > 1000) return false;
            solution.scale = scale;
            solution.rotation = Quaternion.Euler(0, (float)System.Math.Atan2(cross, dot) * Mathf.Rad2Deg, 0);
            solution.position = b - solution.rotation * (a * scale);
            float sum = 0;
            for (int i = 0; i < source.Length; i++)
            {
                float error = Vector3.Distance(solution.TransformPoint(source[i]), target[i]);
                solution.maxError = Mathf.Max(solution.maxError, error);
                sum += error * error;
            }
            solution.rmsError = Mathf.Sqrt(sum / source.Length);
            return Finite(solution.position) && Finite(solution.maxError);
        }

        private static float SignedArea(Vector3[] points)
        {
            float area = 0;
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 a = points[i], b = points[(i + 1) % points.Length];
                area += a.x * b.z - b.x * a.z;
            }
            return area;
        }

        private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        private static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
    }
}
