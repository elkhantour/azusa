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
        private Mesh _targetMesh;
        private float _areaSum { get; set; }
        private List<Triangle> _triangles { get; set; } = new List<Triangle>();

        public PointDistributor(GameObject ta, float margin = 0.0f)
        {
            TargetObject = ta;

	    // make a copy of the mesh to freely manipulate it (i.e. shrinking)
            CopyMeshFromGameObject(TargetObject, out _targetMesh);

	    Debug.Log(_targetMesh);
            _margin = margin;

            if (_margin > 0)
            {
                MeshUtils.Shrink(_targetMesh, _margin);
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

        public List<Vector3> GetRandomPoints(int pointNumbers)
        {

            List<Vector3> positions = new List<Vector3>();

	    	    Debug.Log(_targetMesh);
            if (_targetMesh == null)
            {
                Debug.LogWarning("No mesh were found in the provided Game Object. Make sure the Game Object has a valid Mesh Filter component with a Mesh in it.");
                return positions;
            }


            _areaSum = CalculateAreaSum(_targetMesh.triangles);

            for (int i = 0; i < pointNumbers; i++)
            {
                Triangle triangle = RandomTriangle();
                Vector3 randomPoints = RandomWithinTriangle(triangle);
                positions.Add(randomPoints);
            }

            return positions;
        }


        public List<Vector3> GetJitteredGridPoints(float cellSize = 1.0f, float jitter = 0.4f, float stagger = 0.5f)
        {

            List<Vector3> positions = new List<Vector3>();

            MeshFilter meshFilter = TargetObject.GetComponent<MeshFilter>();

            // Transform bounds to world space if the object is moved/scaled
            Vector3 min = _targetMesh.bounds.min;
            Vector3 max = _targetMesh.bounds.max;

            int rowCount = 0;
            float rayStartHeight = max.y + 5.0f; // Start ray above the highest point

            for (float z = min.z; z < max.z; z += cellSize)
            {
                float xOffset = (rowCount % 2 == 0) ? 0 : cellSize * stagger;

                for (float x = min.x + xOffset; x < max.x; x += cellSize)
                {
                    float adjustedPosition = cellSize * jitter;
                    float jitterX = UnityEngine.Random.Range(-adjustedPosition, adjustedPosition);
                    float jitterZ = UnityEngine.Random.Range(-adjustedPosition, adjustedPosition);

                    Vector3 rayOrigin = new Vector3(x + jitterX, rayStartHeight, z + jitterZ);

                    // Cast ray down to find the mesh surface
                    if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit))
                    {
                        // Ensure we actually hit the island mesh and not something else
                        if (hit.collider.gameObject == TargetObject)
                        {
                            positions.Add(hit.point);
                        }
                    }
                }
                rowCount++;
            }


            return positions;
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
            _targetMesh.vertices[triangles[t]],
        _targetMesh.vertices[triangles[t + 1]],
    _targetMesh.vertices[triangles[t + 2]],
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
