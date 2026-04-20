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
    public class SpreadPoints
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

                private set { }
            }
        }

        public Mesh Mesh { get; private set; }
        public int PointsNumber { get; private set; }
        private float AreaSum { get; set; }
        private List<Triangle> Triangles { get; set; } = new List<Triangle>();

        public SpreadPoints(Mesh mesh, int pointNumbers)
        {
            Mesh = mesh;
            PointsNumber = pointNumbers;
        }

        public Vector3[] GeneratePoints()
        {

            AreaSum = CalculateAreaSum(Mesh.triangles);
            List<Vector3> positions = new List<Vector3>();

            for (int i = 0; i < PointsNumber; i++)
            {
                Triangle triangle = RandomTriangle();
                Vector3 randomPoints = RandomWithinTriangle(triangle);
                positions.Add(randomPoints);
            }


            return positions.ToArray();

        }


        /// <summary>
        /// Calculates the area of a triangle using Heron's Formula.
        /// </summary>
        /// <param name="triangle">An array of 3 floats representing the lengths of the triangle's sides.</param>
        /// <returns>The surface area of the triangle. Returns 0.0f if the input is invalid.</returns>
        private float TriangleArea(float[] triangle)
        {
            if (triangle.Length < 3)
            {
                return 0.0f;
            }

            float A = triangle[0];
            float B = triangle[1];
            float C = triangle[2];

            float S = (A + B + C) / 2; //semi perimeter

            return (float)Math.Sqrt(S * (S - A) * (S - B) * (S - C));
        }

        /// <summary>
        /// Calculates the total surface area of the mesh and populates the triangle cache.
        /// </summary>
        /// <param name="triangles">The index buffer array from the mesh.</param>
        /// <returns>The total combined area of all triangles in the mesh.</returns>
        /// <remarks>
        /// This method acts as an initializer for the 'Triangles' list, which is required for weighted sampling.
        /// </remarks>
        private float CalculateAreaSum(int[] triangles)
        {
            float sum = 0.0f;

            for (int t = 0; t < triangles.Length; t += 3)
            {
                Triangle triangle = new Triangle()
                {
                    Vertices = new int[3]
                    {
                        triangles[t],
                        triangles[t + 1],
                        triangles[t + 2]
                    },
                    Points = new Vector3[3] {
                       Mesh.vertices[ triangles[t] ],
                       Mesh.vertices[ triangles[t+1] ],
                       Mesh.vertices[ triangles[t+2] ]
                    }
                };


                triangle.Area = TriangleArea(triangle.Edges);

                //Store to cache
                Triangles.Add(triangle);

                sum += triangle.Area;
            }


            return sum;
        }

        /// <summary>
        /// Selects a random triangle from the processed collection using weighted probability.
        /// </summary>
        /// <returns>A Triangle object. Larger triangles have a higher probability of being selected.</returns>
        /// <seealso cref="CalculateAreaSum"/>
        private Triangle RandomTriangle()
        {

            float rng = UnityEngine.Random.Range(0f, AreaSum);
            foreach (Triangle triangle in Triangles)
            {
                if (rng < triangle.Area)
                {
                    return triangle;
                }

                rng -= triangle.Area;
            }

            return Triangles.Last();

        }

        /// <summary>
        /// Computes a uniform random point within the bounds of a specific triangle.
        /// </summary>
        /// <param name="tri">The triangle data containing vertex positions.</param>
        /// <returns>A position in local space within the triangle.</returns>
        /// <remarks>
        /// Uses Barycentric coordinate math with a square-root transform to ensure a uniform distribution.
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

