using System.Collections.Generic;
using UnityEngine;
using Triangulation;


namespace Utils
{
    /// <summary>
    /// Basic utilities for Mesh manipulation (similar to Blender modifiers)
    /// </summary>
    /// <remarks>
    /// WARNING: The mesh utils is destructive and will alter the provided mesh in argument.
    /// A Clone method is available if there is the necessity to keep the original mesh untouched.
    /// </remarks>
    public static class MeshUtils
    {

        public static Mesh DownSample(Mesh mesh, int max)
        {

            Mesh tempMesh = new Mesh();

            if (mesh.vertices.Length > max)
            {
                List<Vector3> vertices = new List<Vector3>();

                for (int i = 0; i < mesh.vertices.Length; i += Mathf.CeilToInt(mesh.vertices.Length / max))
                {
                    vertices.Add(mesh.vertices[i]);
                }

                tempMesh.vertices = vertices.ToArray();
            }

            return tempMesh;
        }

        /// <summary>
        /// Downsamples a mesh by clustering vertices within a grid cell.
        /// </summary>
        /// <param name="originalMesh">The source mesh to downsample.</param>
        /// <param name="cellSize">The size of the grid cell. Larger values result in lower poly counts.</param>
        /// <returns>A new, simplified Mesh object.</returns>
        public static Mesh Decimate(Mesh originalMesh, float cellSize, bool triangulate = true)
        {
            if (originalMesh == null) return null;
            if (cellSize <= 0) return originalMesh;

            Vector3[] vertices = originalMesh.vertices;
            int[] triangles = originalMesh.triangles;
            Vector2[] uvs = originalMesh.uv;

            Dictionary<Vector3Int, int> grid = new Dictionary<Vector3Int, int>();
            List<Vector3> newVertices = new List<Vector3>();
            List<Vector2> newUvs = new List<Vector2>();
            int[] vertexMap = new int[vertices.Length];

            // 1. Cluster vertices into grid cells
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3Int cellKey = new Vector3Int(
                    Mathf.RoundToInt(vertices[i].x / cellSize),
                    Mathf.RoundToInt(vertices[i].y / cellSize),
                    Mathf.RoundToInt(vertices[i].z / cellSize)
                );

                if (!grid.TryGetValue(cellKey, out int newIndex))
                {
                    newIndex = newVertices.Count;
                    grid.Add(cellKey, newIndex);
                    newVertices.Add(vertices[i]);
                    if (uvs.Length > 0) newUvs.Add(uvs[i]);
                }
                vertexMap[i] = newIndex;
            }

            // 2. Rebuild triangles using the new vertex indices
            List<int> newTriangles = new List<int>();
            if (triangulate)
            {
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int i1 = vertexMap[triangles[i]];
                    int i2 = vertexMap[triangles[i + 1]];
                    int i3 = vertexMap[triangles[i + 2]];

                    // Only add the triangle if it hasn't become degenerate (all points same)
                    if (i1 != i2 && i1 != i3 && i2 != i3)
                    {
                        newTriangles.Add(i1);
                        newTriangles.Add(i2);
                        newTriangles.Add(i3);
                    }
                }
            }

            // 3. Construct the resulting mesh
            Mesh simplifiedMesh = new Mesh();
            simplifiedMesh.name = originalMesh.name + "_Downsampled";
            simplifiedMesh.vertices = newVertices.ToArray();
            simplifiedMesh.triangles = newTriangles.ToArray();
            if (newUvs.Count > 0) simplifiedMesh.uv = newUvs.ToArray();

            simplifiedMesh.RecalculateNormals();
            simplifiedMesh.RecalculateBounds();

            return simplifiedMesh;
        }

        /// <summary>
        /// Convert a vector 3 to a vector 2 by removing the Y axis
        /// </summary>
        public static Vector2[] ToVector2(Vector3[] vertices)
        {

            List<Vector2> vectors = new List<Vector2>();

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 currVert = vertices[i];
                vectors.Add(new Vector2(currVert.x, currVert.z));
            }

            return vectors.ToArray();

        }

        /// <summary>
        /// Converts a vector 2 to a vector 3 by insterting the second argument as the Y dimension.
        /// </summary>
        public static Vector3[] ToVector3(Vector2[] vertices, float y = 0.0f)
        {

            List<Vector3> vectors = new List<Vector3>();

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector2 currVert = vertices[i];
                vectors.Add(new Vector3(currVert.x, y, currVert.y));
            }

            return vectors.ToArray();

        }

        /// <summary>
        /// Offset down a mesh by computing vertices bisect axes.
        /// </summary>
        public static Mesh Offset(Mesh mesh, float distance)
        {

            List<Vector3> vertices = new List<Vector3>();

            //Get bisect axes and shrink down mesh
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                Vector3 A = mesh.vertices[i == 0 ? mesh.vertices.Length - 1 : i - 1];
                Vector3 B = mesh.vertices[i];
                Vector3 C = mesh.vertices[(i + 1) % mesh.vertices.Length];

                Vector3 AB = B - A;
                Vector3 BC = C - B;

                Vector3 bisector_norm = (B + AB + BC).normalized;

                Vector3 dir = distance * (B - bisector_norm).normalized;
                Vector3 shrinkPoint = dir + B;
                vertices.Add(shrinkPoint);
            }


            mesh.vertices = vertices.ToArray();
            mesh.RecalculateBounds();

            return mesh;

        }


        public static Mesh Clone(Mesh src)
        {
            Mesh dest = new Mesh();
            dest.name = src.name + " (Copy)";

            dest.vertices = src.vertices;
            dest.triangles = src.triangles;
            dest.uv = src.uv;
            dest.normals = src.normals;

            dest.RecalculateNormals();
            dest.RecalculateBounds();

            return dest;
        }


        public static Bounds BoundingBox(Mesh mesh)
        {
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;


            foreach (Vector3 vertex in mesh.vertices)
            {
                minX = Mathf.Min(minX, vertex.x);
                minY = Mathf.Min(minY, vertex.y);
                maxX = Mathf.Max(maxX, vertex.x);
                maxY = Mathf.Max(maxY, vertex.y);
            }

            return new Bounds(
                new Vector2((minX + maxX) / 2, (minY + maxY) / 2),
                new Vector2(maxX - minX, maxY - minY)
            );

        }

        public static void OffsetVertices(Mesh mesh, Vector3 offset)
        {

            Vector3[] vertices = mesh.vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] += offset;
            }

            mesh.vertices = vertices;
            mesh.RecalculateBounds();

        }

        /// <summary>
        /// Returns a new mesh with duplicate vertices merged.
        /// Works with any MeshTopology (Lines, Triangles, etc.).
        /// O(n²) — fine for outline meshes, use a spatial hash for large meshes.
        /// </summary>
        public static Mesh RemoveDuplicateVertices(Mesh source, float epsilon = 0.0001f)
        {
            Vector3[] oldVerts = source.vertices;
            int[] oldIndices = source.GetIndices(0);
            float epsSq = epsilon * epsilon;

            int[] remap = new int[oldVerts.Length];
            List<Vector3> newVerts = new List<Vector3>(oldVerts.Length);

            for (int i = 0; i < oldVerts.Length; i++)
            {
                int found = -1;
                for (int j = 0; j < newVerts.Count; j++)
                {
                    if ((oldVerts[i] - newVerts[j]).sqrMagnitude < epsSq)
                    {
                        found = j;
                        break;
                    }
                }

                if (found >= 0)
                    remap[i] = found;
                else
                {
                    remap[i] = newVerts.Count;
                    newVerts.Add(oldVerts[i]);
                }
            }

            int[] newIndices = new int[oldIndices.Length];
            for (int i = 0; i < oldIndices.Length; i++)
                newIndices[i] = remap[oldIndices[i]];

            Mesh mesh = new Mesh { name = source.name };
            mesh.SetVertices(newVerts);
            mesh.SetIndices(newIndices, source.GetTopology(0), 0);
            mesh.RecalculateBounds();
            return mesh;
        }


        /// <summary>
        /// Re-winds a set of deduplicated vertices into a consistent loop order
        /// by repeatedly picking the nearest unvisited neighbor, starting from index 0.
        /// </summary>
        public static void RewindLoop(Mesh mesh)
        {

            Vector3[] unorderedLoop = mesh.vertices;
            List<Vector3> remaining = new List<Vector3>(unorderedLoop);
            Vector3[] ordered = new Vector3[unorderedLoop.Length];
            ordered[0] = remaining[0];
            remaining.RemoveAt(0);

            for (int i = 1; i < ordered.Length; i++)
            {
                float bestDist = float.MaxValue;
                int bestIdx = 0;
                for (int j = 0; j < remaining.Count; j++)
                {
                    float d = (ordered[i - 1] - remaining[j]).sqrMagnitude;
                    if (d < bestDist) { bestDist = d; bestIdx = j; }
                }
                ordered[i] = remaining[bestIdx];
                remaining.RemoveAt(bestIdx);
            }

            mesh.vertices = ordered;
        }


    }

}
