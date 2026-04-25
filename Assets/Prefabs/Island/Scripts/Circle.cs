using System.Collections.Generic;
using UnityEngine;
using Triangulation;
using Utils;
using System.Linq;

/*
 Circles are the base armature of the islands, they are simply 2d circles deformed
 with noise and that can be triangulated
 */


namespace Island
{

    public interface ICircle
    {
        public int Segments { get; set; }
        public float Radius { get; set; }
        public float NoiseScale { get; set; }
        public float NoiseAmplitude { get; set; }
        public float Randomness { get; set; }
        public Vector3 Position { get; set; }
        public string Name { get; set; }
    }


    public class Circle : ICircle
    {

        public int Segments { get; set; } = 30;
        public float Radius { get; set; } = 1f;
        public float NoiseScale { get; set; } = 0.58f;
        public float NoiseAmplitude { get; set; } = 13.41f;
        public float Randomness { get; set; } = 1.3f;
        public Vector3 Position { get; set; } = new Vector3(0, 0, 0);
        public string Name { get; set; }
        public bool DebugMode { get; set; } = false;
        public bool Smooth { get; set; } = false;
        public int SmoothThresholdAngle { get; set; } = 110;
        public Color InnerVertexColor { get; set; } = Color.black;

        public Vector3[] OuterVertices { get; private set; } = new Vector3[] { };
        public Vector3[][] InnerVertices { get; private set; } = new Vector3[][] { };

        public float[] InnerCircles { get; set; } = new float[] { };

        public Mesh Mesh { get; private set; } = new Mesh();

        public void Spawn()
        {

            //Generate Outer Ring
            Vector2[] points = Points();
            if (Smooth) points = SmoothPeak(points, SmoothThresholdAngle);

            OuterVertices = MeshUtils.ToVector3(points);

            //Generate Inner Ring
            if (InnerCircles.Length > 0)
            {
                List<Vector3[]> inners = new List<Vector3[]>();
                for (int i = 0; i < InnerCircles.Length; i++)
                {
                    Vector2[] shrinkCircle = GenerateShrinkCircles(points, InnerCircles[i]);
                    //Adding Inner ring to global array
                    inners.Add(MeshUtils.ToVector3(shrinkCircle));
                }
                InnerVertices = inners.ToArray();
            }


            Mesh = CombineRings();
            Mesh.uv = Uv.Planar(Mesh.vertices);
            Mesh.normals = Normal.Set(Mesh);
            Mesh.name = Name;
            Mesh.RecalculateNormals();
	    Mesh.RecalculateBounds();
	    
            SetPosition(Position);
            SetInnerVertexColor(InnerVertexColor);

            //mesh.normals = Normals(mesh.vertices);
            //mesh.uv = Uvs(mesh.vertices);


            if (DebugMode)
            {
                DebugCircle();
            }

        }

        private Mesh CombineRings()
        {

            //Generate triangulation from concating outer + inner vertices
            List<Vector3> outerInner = new List<Vector3>();
            outerInner = outerInner.Concat(OuterVertices).ToList();
            for (int i = 0; i < InnerVertices.Length; i++)
            {
                outerInner = outerInner.Concat(InnerVertices[i]).ToList();
            }

            //Generate triangulation and add the outervertices as outer edges, so during clean up we keep inner ring triangles (preventing having to merge our different ring layers)
            Triangulator triangulator = new Triangulator(MeshUtils.ToVector2(outerInner.ToArray()), MeshUtils.ToVector2(OuterVertices));
            return triangulator.Mesh;
        }

        private Vector2[] SmoothPeak(Vector2[] points, float threshold)
        {

            List<Vector2> smoothedPoints = new List<Vector2>();
            int nPeak = 0;


            //prefill list
            for (int i = 0; i < points.Length; i++)
            {
                smoothedPoints.Add(points[i]);
            }

            //Check the angle of each 3 vertices
            //If angle < threshold then add two more vertices between and make peak point closer to ease
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 A = points[i];
                Vector2 B = points[(i + 1) % points.Length];
                Vector2 C = points[(i + 2) % points.Length];

                Vector2 AB = A - B;
                Vector2 BC = C - B;

                bool isPeak = Vector2.Angle(AB, BC) < threshold;

                if (isPeak)
                {

                    Vector2 halfAB = (A + B) / 2;
                    Vector2 halfBC = (C + B) / 2;

                    if (smoothedPoints.Count > i + nPeak + 1)
                    {
                        smoothedPoints[i + nPeak + 1] = (halfAB + B + halfBC) / 3;
                        smoothedPoints.Insert(i + nPeak + 1, halfAB);

                        if (smoothedPoints.Count > i + nPeak + 3)
                        {
                            smoothedPoints.Insert(i + nPeak + 3, halfBC);
                        }
                    }

                    nPeak += 2;
                }

            }

            return smoothedPoints.ToArray();
        }


        private void DebugCircle()
        {

            Debugger.Polygon(new Polygon()
            {
                Points = Mesh.vertices
            });

            for (int i = 0; i < Mesh.vertices.Length; i++)
            {
                Debugger.Label(new Label()
                {
                    Text = "" + i,
                    Position = Mesh.vertices[i] + new Vector3(0, 1, 0)
                });
            }
        }


        private Vector2[] Points()
        {

            //Setup initial points from which the delaunay triangulation will be calculated
            List<Vector2> pts = new List<Vector2>();

            float angleStep = 360.0f / Segments;
            for (int i = 0; i < Segments; i++)
            {

                //generate base circle coordinates
                float angle = Mathf.Deg2Rad * (i * angleStep);
                float x = Mathf.Cos(angle) * Radius;
                float z = Mathf.Sin(angle) * Radius;

                //add noise
                float noise = Mathf.PerlinNoise(x * NoiseScale, z * NoiseScale) * NoiseAmplitude;
                float adjustedRadius = Radius + noise;
                adjustedRadius += Random.Range(-Randomness, Randomness);

                pts.Add(new Vector2(x * adjustedRadius, z * adjustedRadius));
            }


            return pts.ToArray();
        }

        private Vector2[] GenerateShrinkCircles(Vector2[] pts, float distance)
        {

            Mesh tempMesh = new Mesh();
            Vector3[] vertices = MeshUtils.ToVector3(pts);

            tempMesh.vertices = vertices;

            Mesh shrinkMesh = MeshUtils.Shrink(tempMesh, distance);
            shrinkMesh = MeshUtils.DownSample(shrinkMesh, Mathf.FloorToInt(shrinkMesh.vertices.Length / 2));

            return MeshUtils.ToVector2(shrinkMesh.vertices);

        }


        private int[] DefaultTriangles()
        {

            List<int> tris = new List<int>();
            // Triangles
            for (int i = 1; i <= Segments; i++)
            {
                int nextIndex = (i % Segments) + 1;

                // Triangle made of the center vertex, current vertex, and next vertex
                tris.Add(0); // Center vertex
                tris.Add(nextIndex);
                tris.Add(i);
            }

            return tris.ToArray();
        }

        private Vector3[] DefaultVertices(Vector2[] points)
        {
            List<Vector3> vertices = new List<Vector3>();

            vertices.Add(Vector3.zero);
            // Calculate the vertices around the circle
            for (int p = 0; p < points.Length; p++)
            {
                Vector2 currentPoint = points[p];
                vertices.Add(new Vector3(currentPoint.x, 0, currentPoint.y));
            }

            return vertices.ToArray();
        }

        public void SetPosition(Vector3 position)
        {

            //Update Mesh Vertices
            Vector3[] tempVert = Mesh.vertices;
            for (int i = 0; i < Mesh.vertices.Length; i++)
            {
                tempVert[i] += position;
            }

            Mesh.vertices = tempVert;
            Mesh.RecalculateBounds();

            //Update OuterVertices
            for (int i = 0; i < OuterVertices.Length; i++)
            {
                OuterVertices[i] += position;
            }

            //Update InnerVertices
            for (int i = 0; i < InnerVertices.Length; i++)
            {
                for (int v = 0; v < InnerVertices[i].Length; v++)
                {
                    InnerVertices[i][v] += position;
                }
            }

        }


        private void SetInnerVertexColor(Color color)
        {
            List<Color> colors = new List<Color>();

            for (int i = 0; i < Mesh.vertices.Length; i++)
            {
                colors.Add(i < OuterVertices.Length ? Color.white : Color.black);
            }

            Mesh.colors = colors.ToArray();
        }

        public void UpdateOuterVertices(Vector3[] vertices)
        {
            OuterVertices = vertices;
        }
    }


}
