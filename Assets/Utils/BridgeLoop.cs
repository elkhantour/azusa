using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Utils
{

    // DELETEME
    public class SuperiorPoint : Point
    {
        public Point Connection { get; set; }
    }

    public class InferiorPoint : Point
    {
        public List<SuperiorPoint> Connections { get; set; }
    }

    public static class BridgeLoop
    {

        private static float Distance(Vector3 A, Vector3 B)
        {
            return Mathf.Sqrt(
                (float)Math.Pow(B.x - A.x, 2)
                + (float)Math.Pow(B.y - A.y, 2)
                + (float)Math.Pow(B.z - A.z, 2)
               );
        }

        private static Point ClosestPoint(Vector3 point, Vector3[] target)
        {

            int closestIndex = 0;
            Vector3 closestPosition = Vector3.zero;

            for (int i = 0; i < target.Length; i++)
            {

                if (closestPosition == Vector3.zero
                    || Distance(point, target[i]) < Distance(point, closestPosition))
                {
                    closestIndex = i;
                    closestPosition = target[i];
                }
            }

            return new Point()
            {
                Index = closestIndex,
                Position = closestPosition
            };

        }


        private static Mesh Segment(Mesh mesh, List<Edge> edges, int segmentAmount)
        {

            List<Vector3> segmentsVertices = mesh.vertices.ToList();

            for (int s = 0; s < segmentAmount; s++)
            {
                //place points every 1/segments amount
                foreach (Edge edge in edges)
                {
                    segmentsVertices.Add(edge.Begin.Position + s / segmentAmount * (edge.End.Position - edge.Begin.Position));
                }

            }

            return mesh;

        }



        public static Mesh Connect(Vector3[] origin, Vector3[] target, int segments = 0, bool debug = false)
        {
            Mesh tempMesh = new Mesh();
            List<Vector3> combinedPoints = new List<Vector3>();
            List<Edge> edges = new List<Edge>();
            List<int> triangles = new List<int>();
            //List<Point> segments = new List<Point>();


            /* 
             * ===================== VERTICES ===================
             * Simply combine origin and target points
             */

            combinedPoints.AddRange(origin);
            combinedPoints.AddRange(target);

            //add combined vertices to temporary mesh
            tempMesh.vertices = combinedPoints.ToArray();


            if (debug)
            {

                Debugger.Polygon(new Polygon()
                {
                    Points = tempMesh.vertices,
                    Edges = false
                });

                for (int i = 0; i < tempMesh.vertices.Length; i++)
                {
                    Debugger.Label(new Label()
                    {
                        Text = "" + i,
                        Position = tempMesh.vertices[i] + new Vector3(0, 1, 0)
                    });
                }
            }


            /*
             * ===================== CLUSTER ===================
             * Segment "Superior" points depending on their proximity to Inferior points
             *             ____________________
             *            |                    |
             * Superior   |  [0][1][2][3][4]   |    [5][6][7][8][9]
             *            |    \__\_|_/__/     |       \__\_|_/__/
             *            |         V          |            V
             * Inferior   |        [0]         |           [1] ....
             *            |____________________|
             *                    Cluster
             * Inferior becomes our Connection Points Target from which the superior points will
             * have to "adapt" themeselves to the available points from inferior
             * 
             * 
             *
            */


            Vector3[] superior = origin.Length > target.Length ? origin : target;
            Vector3[] inferior = origin.Length > target.Length ? target : origin;

            InferiorPoint[] inferiorPoints = new InferiorPoint[inferior.Length];

            /*1. Fill up inferior points array
             * 
             * Note: since we merged our target and origin, the inferior vextices index have been updated,
             * the inferior index have been concat to the superior index, 
             * so we need to take this in consideration when assigning the Index value of inferior point
             * that is not anymore "0" but "0 + superior.Length" as instance
             * 
             */

            for (int i = 0; i < inferiorPoints.Length; i++)
            {
                inferiorPoints[i] = new InferiorPoint()
                {
                    Index = superior.Length + i,
                    Position = inferior[i],
                    Connections = new List<SuperiorPoint>()
                };
            }

            //2. First pass to connect Superior --to--> Inferior depending on their proximity (Harmonisation Phase)
            for (int i = 0; i < superior.Length; i++)
            {

                /* Get the closest vertex index for each of our points
		 * 
		 *  
		 *      [0]----[1]----[2]
		 *        \    /   ___/
		 *         \  /___/ 
		 *  [10----[9]----------------------[8]
		 *  
		 */


                Point connection = ClosestPoint(superior[i], inferior);
                SuperiorPoint verti = new SuperiorPoint()
                {
                    Index = i,
                    Position = superior[i],
                    Connection = connection
                };



                /*
                 * Increment connection number value in our connection points array
                 * as to later on detect if there are orphans or solo connected
                */


                inferiorPoints[connection.Index].Connections.Add(verti);


            }


            /**
             * 3. Second pass to check orphan inferior points (one that have either 1 or 0 connections)
             * Since unity mesh works with triangle we need to ensure all point have at least 2 connections
             * as to prevent holes in our mesh (Completion Phase)
             * 
             * Note: since merge our target and origin, the vextex index has been updated,
             * the inferior index have been added to the superior index so we
             * need to take this in consideration when looping
             * 
             */

            for (int i = 0; i < inferiorPoints.Length; i++)
            {
                InferiorPoint currentPoint = inferiorPoints[i];

                if (currentPoint.Connections.Count == 0)
                {
                    //Retrieve superior closest point
                    Point closestSuperiorPoint = ClosestPoint(currentPoint.Position, superior);

                    //Add new Superior Point to our Inferior Connection List
                    currentPoint.Connections.Add(new SuperiorPoint()
                    {
                        Index = closestSuperiorPoint.Index,
                        Position = closestSuperiorPoint.Position,
                        Connection = new Point() { Index = currentPoint.Index, Position = currentPoint.Position }
                    });
                }

            }


            /**
             * 4. Automatically link up Inferior Point to previous latest Cluster Superior (n-1)
             * 
             * ]--[3]   [4]---[5]--[6]
             *   /  \__   
             *  /      \_   
             * ]         \      
             *            [2]
             */

            for (int i = 0; i < inferiorPoints.Length; i++)
            {
                InferiorPoint currentPoint = inferiorPoints[i];
                InferiorPoint previousPoint = inferiorPoints[i == 0 ? inferiorPoints.Length - 1 : i - 1];

                if (i == 1)
                {
                    //Debug.Log("current :"+string.Join(",", currentPoint.Connections.Select(x => x.Index)));
                    //Debug.Log("previous :"+string.Join(",", previousPoint.Connections.Select(x => x.Index)));
                }

                //Edgecase (NOT RESOLVED)
                if (i == 1 && previousPoint.Connections.Count >= 3)
                {

                    /*
                     * The biggest value of half of the length
                     *
                     */

                    SuperiorPoint maxBelowHalfConnection = previousPoint.Connections[0];
                    foreach (SuperiorPoint sup in previousPoint.Connections)
                    {

                        if (sup.Index < superior.Length / 2)
                        {
                            //If current maxBelowHalfConnection Index is above Half, automatically assign this new point that is below half
                            if (maxBelowHalfConnection.Index > superior.Length / 2)
                            {
                                maxBelowHalfConnection = sup;
                            }
                            else if (sup.Index > maxBelowHalfConnection.Index) //Else if maxBelowHalf is below half, check which Index is bigger
                            {
                                maxBelowHalfConnection = sup;
                            }
                        }
                    }

                    //Debug.Log("connect to \t"+maxBelowHalfConnection.Index);

                    currentPoint.Connections.Insert(0, maxBelowHalfConnection);

                }
                else
                {
                    currentPoint.Connections.Insert(0, previousPoint.Connections.Last());
                }

                //Debug.Log($"{i} \t [{string.Join(",", currentPoint.Connections.Select(x => x.Index) )}]");

                //Generate triangles and edges
                for (int c = 0; c < currentPoint.Connections.Count; c++)
                {

                    Point currentConnection = currentPoint.Connections[c];

                    //Create edges
                    edges.Add(new Edge()
                    {
                        Begin = currentConnection,
                        End = currentPoint
                    });


                    //Create Triangle Part A
                    if (c < currentPoint.Connections.Count - 1)
                    {
                        Point nextConnection = currentPoint.Connections[c + 1];
                        //Create Triangle
                        triangles.Add(currentPoint.Index);
                        triangles.Add(currentConnection.Index);
                        triangles.Add(nextConnection.Index);

                        if (i == inferiorPoints.Length - 1)
                        {
                            //Debug.Log(currentPoint.Index);
                        }
                    }


                    if (debug)
                    {

                        Debugger.Polygon(new Polygon()
                        {
                            Points = new Vector3[]{
                            currentPoint.Position,
                            currentConnection.Position
                        }
                        });

                        Debugger.Label(new Label()
                        {
                            Text = "" + currentConnection.Index,
                            Position = currentConnection.Position + new Vector3(0, 1, 0)
                        });


                        Debugger.Label(new Label()
                        {
                            Text = "" + currentPoint.Index,
                            Position = currentPoint.Position + new Vector3(0, 1, 0)
                        });
                    }



                }

                //Generate upward rectangles
                triangles.Add(currentPoint.Index);
                triangles.Add(previousPoint.Index);
                triangles.Add(currentPoint.Connections.First().Index);

            }

            tempMesh.triangles = triangles.ToArray();


            //Generate uvs (cylindrical projections
            tempMesh.uv = Uv.Cylindrical(tempMesh.vertices);

            //Generate normals
            tempMesh.normals = Normal.Set(tempMesh);

            tempMesh.RecalculateNormals();

            //Segment connections into strats
            //tempMesh = Segment(tempMesh);

            return tempMesh;
        }




        /*










         */


        /// <summary>
        /// Bridges two Vector3 loops into a Unity Mesh.
        /// Both loops are treated as closed rings (last vertex connects back to first).
        /// If the loops have different vertex counts, the smaller loop is resampled
        /// proportionally to match the larger one.
        /// </summary>
        /// <param name="loopA">First edge loop (e.g. the "bottom" ring).</param>
        /// <param name="loopB">Second edge loop (e.g. the "top" ring).</param>
        /// <param name="flipNormals">Flip triangle winding (normals face inward) if true.</param>
        /// <returns>A Unity Mesh spanning the two loops.</returns>
        public static Mesh CreateBridge(Vector3[] loopA, Vector3[] loopB, bool flipNormals = false)
        {
            int countA = loopA.Length;
            int countB = loopB.Length;

            // Vertices: both loops concatenated as-is, no resampling
            Vector3[] vertices = new Vector3[countA + countB];
            Vector2[] uvs = new Vector2[countA + countB];

            for (int i = 0; i < countA; i++)
            {
                vertices[i] = loopA[i];
                uvs[i] = new Vector2((float)i / countA, 0f);
            }
            for (int i = 0; i < countB; i++)
            {
                vertices[countA + i] = loopB[i];
                uvs[countA + i] = new Vector2((float)i / countB, 1f);
            }

            // Proportional stepping (Bresenham-style)
            List<int> tris = new List<int>();
            int a = 0, b = 0;
            // Accumulator: tracks which loop is "more overdue" for its next vertex
            while (a < countA || b < countB)
            {

                int a0 = a % countA, a1 = (a + 1) % countA;
                int b0 = countA + b % countB, b1 = countA + (b + 1) % countB;


                // Determine which loop is "behind" proportionally
                // Scaled comparison: (a / countA) vs (b / countB)
                // → cross-multiply to stay in integers: a * countB vs b * countA
                int lhs = a * countB;
                int rhs = b * countA;

                if (lhs == rhs) // perfectly in sync → quad
                {
                    EmitQuad(tris, a0, b0, a1, b1, flipNormals);
                    a++; b++;
                }
                else if (lhs > rhs) // B is behind → triangle advancing B
                {
                    EmitTri(tris, b0, a0, b1, flipNormals);
                    b++;
                }
                else // A is behind → triangle advancing A
                {
                    EmitTri(tris, a0, b0, a1, flipNormals);
                    a++;
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "BridgedLoop";
            mesh.indexFormat = (vertices.Length > 65535)
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void EmitQuad(List<int> tris, int a0, int b0, int a1, int b1, bool flip)
        {
            if (!flip)
            {
                tris.Add(a0); tris.Add(b0); tris.Add(a1);
                tris.Add(a1); tris.Add(b0); tris.Add(b1);
            }
            else
            {
                tris.Add(a0); tris.Add(a1); tris.Add(b0);
                tris.Add(a1); tris.Add(b1); tris.Add(b0);
            }
        }

        private static void EmitTri(List<int> tris, int a0, int b0, int x1, bool flip)
        {
            if (!flip)
            {
                tris.Add(a0); tris.Add(b0); tris.Add(x1);
            }
            else
            {
                tris.Add(a0); tris.Add(x1); tris.Add(b0);
            }
        }



    }

}
