using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a bridge mesh (quad strip) between two Vector3 loops
/// by matching each vertex in loopA to its nearest neighbor in loopB.
/// </summary>
public static class MeshBridge
{
    /// <summary>
    /// Creates a bridge mesh between two edge loops by proximity matching.
    /// Normals are computed automatically (outward-facing via cross product).
    /// </summary>
    /// <param name="loopA">First edge loop.</param>
    /// <param name="loopB">Second edge loop.</param>
    /// <returns>A ready-to-assign Unity Mesh, or null if either loop is too small.</returns>
    public static Mesh CreateBridgeByProximity(Vector3[] loopA, Vector3[] loopB)
    {
        if (loopA == null || loopB == null || loopA.Length < 2 || loopB.Length < 2)
        {
            Debug.LogWarning("[MeshBridge] Both loops must have at least 2 vertices.");
            return null;
        }

        // ------------------------------------------------------------------
        // 1. Proximity matching: for every vertex in loopA, find the nearest
        //    vertex in loopB. Duplicate matches are allowed (unequal loops).
        // ------------------------------------------------------------------
        int[] matchB = new int[loopA.Length];
        for (int i = 0; i < loopA.Length; i++)
        {
            float bestDist = float.MaxValue;
            int   bestIdx  = 0;
            for (int j = 0; j < loopB.Length; j++)
            {
                float d = (loopA[i] - loopB[j]).sqrMagnitude;
                if (d < bestDist) { bestDist = d; bestIdx = j; }
            }
            matchB[i] = bestIdx;
        }

        // ------------------------------------------------------------------
        // 2. Build combined vertex list: loopA first, then loopB.
        //    We keep the original arrays separate so index math stays clean.
        // ------------------------------------------------------------------
        int   countA    = loopA.Length;
        int   countB    = loopB.Length;
        var   vertices  = new Vector3[countA + countB];
        loopA.CopyTo(vertices, 0);
        loopB.CopyTo(vertices, countA);   // loopB indices in mesh = j + countA

        // ------------------------------------------------------------------
        // 3. Generate quads (2 triangles each) for every edge in loopA.
        //    Edge i  -> i+1 in loopA, matched to the corresponding pair in B.
        // ------------------------------------------------------------------
        var triangles = new List<int>(loopA.Length * 6);

        for (int i = 0; i < countA; i++)
        {
            int a0 = i;                         // loopA[i]
            int a1 = (i + 1) % countA;          // loopA[i+1]  (wraps)
            int b0 = matchB[i]         + countA; // loopB match for a0
            int b1 = matchB[(i + 1) % countA] + countA; // loopB match for a1

            // Compute face normal to determine winding (outward-facing).
            Vector3 edge1  = vertices[a1] - vertices[a0];
            Vector3 edge2  = vertices[b0] - vertices[a0];
            Vector3 faceNormal = Vector3.Cross(edge1, edge2);

            // Centroid of the quad.
            Vector3 quadCenter = (vertices[a0] + vertices[a1] + vertices[b0] + vertices[b1]) * 0.25f;

            // Rough outward direction: from the overall bridge center to quad center.
            Vector3 bridgeCenter = ComputeCenter(vertices);
            Vector3 outward      = (quadCenter - bridgeCenter);

            // If the face normal points inward, swap the winding.
            bool flip = Vector3.Dot(faceNormal, outward) < 0f;

            if (!flip)
            {
                // Tri 1: a0, a1, b0
                triangles.Add(a0); triangles.Add(a1); triangles.Add(b0);
                // Tri 2: a1, b1, b0
                triangles.Add(a1); triangles.Add(b1); triangles.Add(b0);
            }
            else
            {
                // Tri 1 flipped: a0, b0, a1
                triangles.Add(a0); triangles.Add(b0); triangles.Add(a1);
                // Tri 2 flipped: a1, b0, b1
                triangles.Add(a1); triangles.Add(b0); triangles.Add(b1);
            }
        }

        // ------------------------------------------------------------------
        // 4. UVs: simple planar unwrap along loop progress (u) and side (v).
        // ------------------------------------------------------------------
        var uvs = new Vector2[vertices.Length];
        for (int i = 0; i < countA; i++)
            uvs[i] = new Vector2((float)i / (countA - 1), 0f);
        for (int j = 0; j < countB; j++)
            uvs[j + countA] = new Vector2((float)j / (countB - 1), 1f);

        // ------------------------------------------------------------------
        // 5. Assemble and return the Mesh.
        // ------------------------------------------------------------------
        Mesh mesh = new Mesh { name = "BridgeMesh" };

        // Use 32-bit indices for large meshes.
        if (vertices.Length > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.vertices  = vertices;
        mesh.triangles = triangles.ToArray();
        mesh.uv        = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Vector3 ComputeCenter(Vector3[] points)
    {
        Vector3 sum = Vector3.zero;
        foreach (var p in points) sum += p;
        return sum / points.Length;
    }
}
