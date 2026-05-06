using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;


namespace Utils
{


    /*
     *Basic utilities for Mesh manipulation (modifiers)
     */
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

        public static Mesh Shrink(Mesh mesh, float distance)
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

                Vector3 dir = -1 * distance * (B - bisector_norm).normalized;
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

	

    }

}
