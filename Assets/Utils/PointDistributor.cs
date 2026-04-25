using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{

    /**
     * @brief Takes in a mesh along with a number of points and proceeds to spread
     * uniformely random points within this mesh.
     */
    public class PointDistributor
    {

        public class Triangle
        {
            public int[] Vertices { get; set; }
            public Vector3[] Points { get; set; }
            public float Area { get; set; }
            public int Index { get; set; }

            public float[] Edges
            {
                get
                {

                    float[] edges = new float[Points.Length];

                    for (int i = 0; i < Points.Length; i++)
                    {
                        Vector3 current = Points[i];
                        Vector3 next = Points[(i + 1) % Points.Length];
                        edges[i] = Vector3.Distance(current, next);
                    }

                    return edges;
                }

                private
                  set { }
            }
        }

        public GameObject TargetObject { get; private set; }
        private float _margin = 0.0f;
        private Mesh _internalMesh;
        private float _areaSum { get; set; }
        private List<Triangle> _triangles { get; set; } = new List<Triangle>();

        public PointDistributor(GameObject ta, float margin = 0.0f)
        {
            TargetObject = ta;

            // make a copy of the mesh to freely manipulate it (i.e. shrinking)
            CopyMeshFromGameObject(TargetObject, out _internalMesh);

            Debug.Log(_internalMesh);
            _margin = margin;

            if (_margin > 0)
            {
                MeshUtils.Shrink(_internalMesh, _margin);
            }
        }

        private void CopyMeshFromGameObject(GameObject obj, out Mesh dest)
        {

            dest = new Mesh();
            MeshFilter mf = obj.GetComponent<MeshFilter>();

            if (mf != null)
            {
                dest.name = mf.mesh.name + " (Copy)";

                dest.vertices = mf.mesh.vertices;
                dest.triangles = mf.mesh.triangles;
                dest.uv = mf.mesh.uv;
                dest.normals = mf.mesh.normals;

                dest.RecalculateNormals();
                dest.RecalculateBounds();
            }
            else
            {
                Debug.LogWarning("Could not find the mesh filter from the provided Game Object.");
            }


        }

        // TODO: Currently we spawn point randomly on each triangle,
        // but couldn't we just randomly spawn points on the bound area
        // an then remove the ones out of the mesh? (like the Jittered Grid method)
        public List<Vector3> GetRandomPoints(int pointNumbers)
        {

            List<Vector3> positions = new List<Vector3>();

            Debug.Log(_internalMesh);
            if (_internalMesh == null)
            {
                Debug.LogWarning("No mesh were found in the provided Game Object. Make sure the Game Object has a valid Mesh Filter component with a Mesh in it.");
                return positions;
            }


            _areaSum = CalculateAreaSum(_internalMesh.triangles);

            for (int i = 0; i < pointNumbers; i++)
            {
                Triangle triangle = RandomTriangle();
                Vector3 randomPoints = RandomWithinTriangle(triangle);
                positions.Add(randomPoints);
            }

            return positions;
        }

        public Dictionary<Vector2Int, Vector3> GetJitteredGridPoints(float cellSize = 1.0f, float jitter = 0.4f, float stagger = 0.5f)
        {
            var gridPoints = new Dictionary<Vector2Int, Vector3>();

            // Get references
            MeshFilter mf = TargetObject.GetComponent<MeshFilter>();
            MeshCollider mc = TargetObject.GetComponent<MeshCollider>();

            if (mf == null || mc == null)
            {
                Debug.LogError("TargetObject needs both a MeshFilter and a MeshCollider!");
                return gridPoints;
            }

            // Store OLD references (use sharedMesh to avoid auto-instantiating copies)
            Mesh originalMesh = mf.sharedMesh;
            Mesh originalColliderMesh = mc.sharedMesh;

            // Swap in the shrunk mesh for the Raycast
            mf.sharedMesh = _internalMesh;
            mc.sharedMesh = _internalMesh;
            // Force Physics to update immediately so the Raycast "sees" the shrunken shape
            Physics.SyncTransforms();

            // Calculate bounds based on the shrunken mesh
            Bounds bounds = _internalMesh.bounds;
            // If object is scaled/moved, transform these to World Space
            Vector3 min = TargetObject.transform.TransformPoint(bounds.min);
            Vector3 max = TargetObject.transform.TransformPoint(bounds.max);

            float rayStartHeight = max.y + 5.0f;

            // Calculate how many steps we need for X and Z
            int colCount = Mathf.CeilToInt((max.x - min.x) / cellSize);
            int rowCount = Mathf.CeilToInt((max.z - min.z) / cellSize);

            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < colCount; c++)
                {
                    // 1. Calculate base position
                    float xBase = min.x + (c * cellSize);
                    float zBase = min.z + (r * cellSize);

                    // 2. Apply Jitter
                    float range = cellSize * jitter;
                    Vector3 rayOrigin = new Vector3(
                        xBase + UnityEngine.Random.Range(-range, range),
                        rayStartHeight,
                        zBase + UnityEngine.Random.Range(-range, range)
                    );

                    // 3. Raycast
                    if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit))
                    {
                        if (hit.collider.gameObject == TargetObject)
                        {
                            // Use Vector2Int as the "Address" of this point
                            gridPoints.Add(new Vector2Int(c, r), hit.point);
                        }
                    }
                }
            }

            // 4. Restore the original state so the island looks normal again
            mf.sharedMesh = originalMesh;
            mc.sharedMesh = originalColliderMesh;
            Physics.SyncTransforms();

            return gridPoints;
        }

        /// <summary>
        /// Calculates the area of a triangle using Heron's Formula.
        /// </summary>
        /// <param name="triangle">An array of 3 floats representing the lengths of
        /// the triangle's sides.</param> <returns>The surface area of the triangle.
        /// Returns 0.0f if the input is invalid.</returns>
        private float TriangleArea(float[] triangle)
        {
            if (triangle.Length < 3)
            {
                return 0.0f;
            }

            float A = triangle[0];
            float B = triangle[1];
            float C = triangle[2];

            float S = (A + B + C) / 2; // semi perimeter

            return (float)Math.Sqrt(S * (S - A) * (S - B) * (S - C));
        }

        /// <summary>
        /// Calculates the total surface area of the mesh and populates the triangle
        /// cache.
        /// </summary>
        /// <param name="triangles">The index buffer array from the mesh.</param>
        /// <returns>The total combined area of all triangles in the mesh.</returns>
        /// <remarks>
        /// This method acts as an initializer for the 'Triangles' list, which is
        /// required for weighted sampling.
        /// </remarks>
        private float CalculateAreaSum(int[] triangles)
        {
            float sum = 0.0f;

            for (int t = 0; t < triangles.Length; t += 3)
            {
                Triangle triangle = new Triangle()
                {
                    Vertices =
                      new int[3] { triangles[t], triangles[t + 1], triangles[t + 2] },
                    Points = new Vector3[3] {
            _internalMesh.vertices[triangles[t]],
        _internalMesh.vertices[triangles[t + 1]],
    _internalMesh.vertices[triangles[t + 2]],
                        }
                };

                triangle.Area = TriangleArea(triangle.Edges);

                // Store to cache
                _triangles.Add(triangle);

                sum += triangle.Area;
            }

            return sum;
        }

        /// <summary>
        /// Selects a random triangle from the processed collection using weighted
        /// probability.
        /// </summary>
        /// <returns>A Triangle object. Larger triangles have a higher probability of
        /// being selected.</returns> <seealso cref="CalculateAreaSum"/>
        private Triangle RandomTriangle()
        {

            float rng = UnityEngine.Random.Range(0f, _areaSum);
            foreach (Triangle triangle in _triangles)
            {
                if (rng < triangle.Area)
                {
                    return triangle;
                }

                rng -= triangle.Area;
            }

            return _triangles.Last();
        }

        /// <summary>
        /// Computes a uniform random point within the bounds of a specific triangle.
        /// </summary>
        /// <param name="tri">The triangle data containing vertex positions.</param>
        /// <returns>A position in local space within the triangle.</returns>
        /// <remarks>
        /// Uses Barycentric coordinate math with a square-root transform to ensure a
        /// uniform distribution.
        /// </remarks>
        private Vector3 RandomWithinTriangle(Triangle tri)
        {
            float r1 = Mathf.Sqrt(UnityEngine.Random.Range(0f, 1f));
            var r2 = UnityEngine.Random.Range(0f, 1f);
            var m1 = 1 - r1;
            var m2 = r1 * (1 - r2);
            var m3 = r2 * r1;

            Vector3 p1 = tri.Points[0];
            Vector3 p2 = tri.Points[1];
            Vector3 p3 = tri.Points[2];

            return (m1 * p1) + (m2 * p2) + (m3 * p3);
        }



    }

}
