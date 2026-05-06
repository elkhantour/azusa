using UnityEngine;

/// <summary>
/// Static utility for extruding a Vector3[] profile along a direction,
/// returning a ready-to-use UnityEngine.Mesh.
/// </summary>
public static class MeshExtruder
{
    /// <summary>
    /// Extrudes a profile (open or closed polygon) along a direction.
    /// </summary>
    /// <param name="profile">Points defining the cross-section in local space.</param>
    /// <param name="direction">World-space direction of extrusion (will be normalized).</param>
    /// <param name="distance">How far to extrude.</param>
    /// <param name="closeProfile">Connect the last point back to the first.</param>
    /// <param name="capEnds">Add flat caps at the start and end (convex profiles only).</param>
    /// <returns>A new Mesh with normals and UVs calculated.</returns>
    public static Mesh Extrude(
        Vector3[] profile,
        Vector3   direction,
        float     distance,
        bool      closeProfile = true,
        bool      capEnds      = true)
    {
        if (profile == null || profile.Length < 2)
            throw new System.ArgumentException("Profile must have at least 2 points.", nameof(profile));

        Vector3 offset    = direction.normalized * distance;
        int     pCount    = profile.Length;
        int     edgeCount = closeProfile ? pCount : pCount - 1;

        // ── Side wall vertices (start ring + end ring) ────────────
        var verts = new System.Collections.Generic.List<Vector3>(pCount * 2);
        for (int i = 0; i < pCount; i++) verts.Add(profile[i]);
        for (int i = 0; i < pCount; i++) verts.Add(profile[i] + offset);

        var uvs  = new System.Collections.Generic.List<Vector2>(verts.Count);
        for (int i = 0; i < pCount; i++) uvs.Add(new Vector2((float)i / (pCount - 1), 0f));
        for (int i = 0; i < pCount; i++) uvs.Add(new Vector2((float)i / (pCount - 1), 1f));

        // ── Side wall triangles ───────────────────────────────────
        var tris = new System.Collections.Generic.List<int>(edgeCount * 6);

        for (int i = 0; i < edgeCount; i++)
        {
            int cur  = i;
            int next = (i + 1) % pCount;
            int curE  = cur  + pCount;
            int nextE = next + pCount;

            tris.Add(cur);  tris.Add(curE);  tris.Add(next);
            tris.Add(next); tris.Add(curE);  tris.Add(nextE);
        }

        // ── Caps (fan triangulation — convex profiles) ────────────
        if (capEnds && closeProfile && pCount >= 3)
        {
            // Start cap — normal points away from offset direction
            int startBase = verts.Count;
            for (int i = 0; i < pCount; i++)
            {
                verts.Add(profile[i]);
                uvs.Add(new Vector2(profile[i].x, profile[i].y));
            }
            for (int i = 1; i < pCount - 1; i++)
            {
                tris.Add(startBase);
                tris.Add(startBase + i + 1);
                tris.Add(startBase + i);
            }

            // End cap — normal points toward offset direction
            int endBase = verts.Count;
            for (int i = 0; i < pCount; i++)
            {
                verts.Add(profile[i] + offset);
                uvs.Add(new Vector2(profile[i].x, profile[i].y));
            }
            for (int i = 1; i < pCount - 1; i++)
            {
                tris.Add(endBase);
                tris.Add(endBase + i);
                tris.Add(endBase + i + 1);
            }
        }

        // ── Build mesh ────────────────────────────────────────────
        var mesh = new Mesh { name = "Extruded" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
