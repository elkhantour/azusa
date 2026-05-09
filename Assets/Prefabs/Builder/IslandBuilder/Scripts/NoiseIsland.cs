using System.Collections.Generic;
using UnityEngine;


namespace Island
{
    namespace Builder
    {
        // ──────────────────────────────────────────────
        //  Data types
        // ──────────────────────────────────────────────

        public enum NoiseMode { XZRadial, XZRadialPlusY, PureY }

        [System.Serializable]
        public class NoiseSettings
        {
            public NoiseMode Mode = NoiseMode.XZRadial;
            public float Frequency = 0.3f;
            public float Amplitude = 0.6f;
            public float YAmplitude = 0.8f;
            public int Octaves = 4;
            public float Persistence = 0.5f;
            public float Lacunarity = 2.0f;
            public Vector3 Offset;
        }


        public class NoiseItem
        {
            public Mesh Mesh;
            public Transform Transform;
            public (int Start, int End) Range;

            // Working array — pulled from Mesh once per flush cycle, shared
            // across jobs that reference the same Mesh instance.
            internal Vector3[] _workingVerts;
        }

        public class NoiseJob
        {
            public string Name;
            public NoiseSettings Noise;
            public List<RadialMask> RadialMasks;
            public List<NoiseItem> Items;
        }

        // ──────────────────────────────────────────────
        //  Static processor
        // ──────────────────────────────────────────────

        public static class NoiseIsland
        {
            /// <summary>
            /// Executes a list of NoiseJobs in order.
            /// All jobs that share the same Mesh instance operate on the same
            /// working array, so successive jobs accumulate correctly.
            /// A single flush pushes every mutated array back to its Mesh at the end.
            /// </summary>
            public static void Apply(List<NoiseJob> jobs)
            {
                // ── Build a shared working-vert cache keyed by Mesh instance ──
                // This ensures two NoiseItems pointing at the same Mesh
                // read and write the same array throughout all jobs.
                var meshCache = new Dictionary<Mesh, Vector3[]>();

                foreach (var job in jobs)
                    foreach (var item in job.Items)
                        if (!meshCache.ContainsKey(item.Mesh))
                            meshCache[item.Mesh] = item.Mesh.vertices; // one copy per Mesh

                // Wire each item to its shared working array
                foreach (var job in jobs)
                    foreach (var item in job.Items)
                        item._workingVerts = meshCache[item.Mesh];

                // ── Validate and execute each job ─────────────────────────────
                foreach (var job in jobs)
                {
                    Validate(job);
                    ExecuteJob(job);
                }

                // ── Flush every mutated array back to its Mesh ────────────────
                foreach (var kvp in meshCache)
                {
                    kvp.Key.vertices = kvp.Value;
                    kvp.Key.RecalculateNormals();
                    kvp.Key.RecalculateBounds();
                }
            }

            // ──────────────────────────────────────────────
            //  Validation
            // ──────────────────────────────────────────────

            static void Validate(NoiseJob job)
            {
                if (job.Items == null || job.Items.Count == 0)
                    throw new System.ArgumentException("NoiseJob has no items.");

                if (job.Items.Count == 1)
                    return; // single item — no seam to validate

                int referenceLength = RangeLength(job.Items[0].Range);

                for (int i = 1; i < job.Items.Count; i++)
                {
                    int len = RangeLength(job.Items[i].Range);
                    if (len != referenceLength)
                        throw new System.ArgumentException(
                            $"NoiseJob '{job.Name}' seam mismatch: Items[0] range length is {referenceLength} " +
                            $"but Items[{i}] range length is {len}. " +
                            $"All items in a job must cover the same number of vertices.");
                }
            }

            // ──────────────────────────────────────────────
            //  Job execution
            // ──────────────────────────────────────────────

            static void ExecuteJob(NoiseJob job)
            {
                int count = RangeLength(job.Items[0].Range);

                for (int i = 0; i < count; i++)
                {
                    // ── Compute the offset once, from the first item's vertex ──
                    // All items in the job are seam-mapped: index i on item 0
                    // corresponds to index i on every other item.
                    var primary = job.Items[0];
                    int primaryIdx = primary.Range.Start + i;
                    Vector3 worldPos = LocalToWorld(primary._workingVerts[primaryIdx], primary.Transform);

                    Vector3 offset = ComputeOffset(worldPos, job.Noise, job.RadialMasks);

                    // ── Apply the same offset to every item at position i ─────
                    foreach (var item in job.Items)
                    {
                        int idx = item.Range.Start + i;
                        Vector3 localOff = WorldOffsetToLocal(offset, item.Transform);
                        item._workingVerts[idx] += localOff;
                    }
                }
            }

            // ──────────────────────────────────────────────
            //  Offset computation
            // ──────────────────────────────────────────────

            static Vector3 ComputeOffset(Vector3 worldPos, NoiseSettings s, List<RadialMask> masks)
            {
                switch (s.Mode)
                {
                    case NoiseMode.XZRadial:
                        {
                            Vector3 dir = XZRadialDirection(worldPos, masks);
                            float mag = SampleNoise(worldPos, s);
                            return dir * mag;
                        }

                    case NoiseMode.XZRadialPlusY:
                        {
                            Vector3 dir = XZRadialDirection(worldPos, masks);
                            float xzMag = SampleNoise(worldPos, s);
                            float yMag = SampleNoise(worldPos + Vector3.up * 7.3f, s);
                            return dir * xzMag + Vector3.down * Mathf.Abs(yMag) * s.YAmplitude;
                        }

                    case NoiseMode.PureY:
                        {
                            float mag = SampleNoise(worldPos + Vector3.up * 7.3f, s);
                            return Vector3.down * Mathf.Abs(mag) * s.YAmplitude;
                        }

                    default:
                        return Vector3.zero;
                }
            }

            // ──────────────────────────────────────────────
            //  Noise direction — XZ only, toward closest mask
            // ──────────────────────────────────────────────

            static Vector3 XZRadialDirection(Vector3 worldPos, List<RadialMask> masks)
            {
                float bestDistSq = float.MaxValue;
                Vector3 bestCenter = worldPos;

                Vector3 flatPos = new Vector3(worldPos.x, 0f, worldPos.z);

                foreach (var mask in masks)
                {
                    Vector3 flatCenter = new Vector3(mask.Position.x, 0f, mask.Position.z);
                    float dSq = (flatPos - flatCenter).sqrMagnitude;

                    if (dSq < bestDistSq)
                    {
                        bestDistSq = dSq;
                        bestCenter = flatCenter;
                    }
                }

                Vector3 diff = flatPos - bestCenter;
                return diff.sqrMagnitude > 0.0001f ? diff.normalized : Vector3.right;
            }

            // ──────────────────────────────────────────────
            //  Perlin noise sampler (layered octaves, XZ only)
            // ──────────────────────────────────────────────

            static float SampleNoise(Vector3 worldPos, NoiseSettings s)
            {
                float value = 0f;
                float amplitude = 1f;
                float frequency = s.Frequency;
                float totalAmp = 0f;

                for (int o = 0; o < s.Octaves; o++)
                {
                    float px = (worldPos.x + s.Offset.x) * frequency;
                    float pz = (worldPos.z + s.Offset.z) * frequency;
                    value += (Mathf.PerlinNoise(px, pz) * 2f - 1f) * amplitude;
                    totalAmp += amplitude;
                    amplitude *= s.Persistence;
                    frequency *= s.Lacunarity;
                }

                return (value / totalAmp) * s.Amplitude;
            }

            // ──────────────────────────────────────────────
            //  Transform helpers
            // ──────────────────────────────────────────────

            static Vector3 LocalToWorld(Vector3 localPos, Transform t)
                => t.localToWorldMatrix.MultiplyPoint3x4(localPos);

            // Offsets are directions/magnitudes, not positions — use MultiplyVector
            // so translation in the matrix doesn't contaminate the displacement.
            static Vector3 WorldOffsetToLocal(Vector3 worldOffset, Transform t)
                => t.worldToLocalMatrix.MultiplyVector(worldOffset);

            // ──────────────────────────────────────────────
            //  Utility
            // ──────────────────────────────────────────────

            static int RangeLength((int Start, int End) range)
                => range.End - range.Start;
        }
    }
}
