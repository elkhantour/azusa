using System;
using System.Collections.Generic;
using Triangulation;
using UnityEngine;
using Utils;

namespace Island
{
    /// <summary>
    /// Generates a procedural underside root-net mesh for a floating island.
    ///
    /// Pipeline:
    ///   1. Read the pre-wound border loop directly from the base mesh vertices.
    ///   2. Poisson-disk sample interior points inside the RadialMask circles.
    ///   3. Delaunay-triangulate all points via Triangulator.Triangulate.
    ///   4. Displace vertices downward with fractal Perlin noise, weighted by
    ///      proximity to RadialMask centres (strongest at centre, zero at border).
    ///   5. Stitch a skirt: connect the border loop to a single apex point.
    ///   6. Return the combined Mesh.
    /// </summary>
    [Serializable]
    public class RootNetMesh
    {
        // ── Settings ─────────────────────────────────────────────────────────

        [SerializeField] private float PoissonRadius = 1.2f;
        [SerializeField] private int PoissonCandidates = 30;
        [SerializeField] private float MaxDepth = 6f;
        [SerializeField] private float NoiseScale = 0.35f;
        [SerializeField] private int NoiseOctaves = 3;
        [SerializeField] private float SkirtDepth = 3f;

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Builds and returns the root-net underside mesh.
        /// </summary>
        /// <param name="baseMesh">The pre-wound marching-squares border mesh.</param>
        /// <param name="radialMasks">Masks that drive displacement weighting.</param>
        public Mesh Generate(Mesh baseMesh, List<RadialMask> radialMasks)
        {
            if (baseMesh == null)
            {
                Debug.LogError("[RootNetMesh] baseMesh is null.");
                return null;
            }

            var noiseSeed = new Vector2(UnityEngine.Random.value * 1000f, UnityEngine.Random.value * 1000f);

            // ── 1. Border loop: already wound by caller, just project to XZ ──
            Vector3[] baseVerts = baseMesh.vertices;
            if (baseVerts.Length < 3)
            {
                Debug.LogError("[RootNetMesh] baseMesh has fewer than 3 vertices.");
                return null;
            }
            var borderLoop = new List<Vector2>(baseVerts.Length);
            foreach (var v in baseVerts)
                borderLoop.Add(new Vector2(v.x, v.z));

            // ── 2. Poisson-disk interior points ──────────────────────────────
            Rect bounds = ComputeBounds(borderLoop);
            List<Vector2> interiorPts = Utils.PoissonDisk.Spawn(bounds, borderLoop, PoissonRadius, PoissonCandidates);

            // ── 3. Build flat point list: [interior ... border ... apex] ─────
            List<Vector2> allXZ = new(interiorPts);
            int borderStart = allXZ.Count;
            allXZ.AddRange(borderLoop);
            int apexIndex = allXZ.Count;
            Vector2 centroid = Centroid(borderLoop);
            allXZ.Add(centroid);

            // ── 4. Delaunay triangulate (interior + border, excluding apex) ──
            var triInput = new Vector3[apexIndex];
            for (int i = 0; i < apexIndex; i++)
                triInput[i] = new Vector3(allXZ[i].x, 0f, allXZ[i].y);

            int[] delaunayTris = Triangulator.Triangulate(triInput);
            delaunayTris = Triangulator.RemoveOuterRingTriangles(delaunayTris, triInput, radialMasks);

            // ── 5. Build 3-D vertices with downward displacement ─────────────
            var verts = new Vector3[allXZ.Count];
            for (int i = 0; i < allXZ.Count; i++)
            {
                Vector2 xz = allXZ[i];
                bool isBorder = i >= borderStart && i < apexIndex;
                bool isApex = i == apexIndex;

                float displacement;
                if (isApex)
                {
                    displacement = MaxDepth + SkirtDepth;
                }
                else if (isBorder)
                {
                    displacement = 0f;
                }
                else
                {
                    float w = MaskWeight(xz, radialMasks);
                    float noise = FractalNoise(xz, NoiseOctaves, NoiseScale, noiseSeed);
                    displacement = w * MaxDepth * noise;
                }


                verts[i] = new Vector3(xz.x, -displacement, xz.y);
            }

            var mesh = new Mesh { name = "RootNetMesh" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(delaunayTris, 0);
            mesh.RecalculateNormals();
            mesh = Normal.Flip(mesh);
            mesh.RecalculateBounds();
            return mesh;
        }

        // ── Noise & Weighting ─────────────────────────────────────────────────

        private static float MaskWeight(Vector2 xz, List<RadialMask> masks)
        {
            if (masks == null || masks.Count == 0) return 0.5f;
            float max = 0f;
            foreach (var mask in masks)
            {
                float dist = Vector2.Distance(xz, new Vector2(mask.Position.x, mask.Position.z));
                float w = Mathf.Clamp01(1f - dist / Mathf.Max(mask.Radius, 0.001f));
                if (w > max) max = w;
            }
            return max;
        }

        private static float FractalNoise(Vector2 xz, int octaves, float scale, Vector2 seed)
        {
            float value = 0f, amplitude = 0.5f, frequency = 1f, max = 0f;
            for (int o = 0; o < octaves; o++)
            {
                float nx = (xz.x + seed.x) * scale * frequency;
                float ny = (xz.y + seed.y) * scale * frequency;
                value += Mathf.PerlinNoise(nx, ny) * amplitude;
                max += amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }
            return value / max;
        }

        // ── Geometry Utilities ────────────────────────────────────────────────

        private static Rect ComputeBounds(List<Vector2> pts)
        {
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in pts)
            {
                if (p.x < minX) minX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.x > maxX) maxX = p.x;
                if (p.y > maxY) maxY = p.y;
            }
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private static Vector2 Centroid(List<Vector2> pts)
        {
            Vector2 sum = Vector2.zero;
            foreach (var p in pts) sum += p;
            return sum / pts.Count;
        }
    }
}
