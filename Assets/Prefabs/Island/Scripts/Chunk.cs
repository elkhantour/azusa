using System;
using UnityEngine;
using System.Collections.Generic;
using Utils;
using System.Linq;

/*
 Chunks are the independant component of an island.
 A chunk basically stacks up many Circles. Chunks turns the 2D basic circles into a SOLO independant island.
 */

namespace Island
{

    public class Chunk
    {

        //Base Circle Configuration
        public int segments = 30;
        public float radius = 1f;

        //Chunk depth
        public float Depth = 3;
        public List<Vector3> vertices = new List<Vector3>();
        public List<Circle> Circles { get; private set; } = new List<Circle>();
        public List<BridgeLoop> BridgeLoops { get; private set; } = new List<BridgeLoop>();
        public Bounds Bounds;

        public Mesh Mesh
        {
            get
            {

                CombineInstance[] combine = new CombineInstance[4];
                Mesh finalMesh = new Mesh();

                List<Circle> circleConfig = new List<Circle>()
                {
                    //1. Set Ground
                    new Circle(){
                        name = "ground",
                        segments = segments,
                        radius = radius,
                        Smooth = true,
                        SmoothThresholdAngle = 160,
                        InnerCircles = new float[] { 1.0f },
                    },

                    //2. Set Belt
                    new Circle()
                    {
                        name= "belt",
                        segments = (int) Mathf.Ceil(segments / 2),
                        radius = radius / 1.2f,
                        position = new Vector3(0, -1 * Depth, 0),
                        Smooth = false
                    },

                    //3. Set Root
                    new Circle()
                    {
                        name = "root",
                        segments = (int)Mathf.Ceil(segments / 3),
                        radius = radius / 2.5f,
                        position = new Vector3(0, -2 * Depth, 0),
                    }
                };


                //Loop through circles to make them spawn 
                foreach (Circle circle in circleConfig)
                {

                    circle.Spawn();

                    //add circle to global array
                    Circles.Add(circle);

                    //Add ground to combine instance
                    if (circle.name == "ground")
                    {
                        combine[0].mesh = circle.Mesh;
                    }
                    if (circle.name == "root")
                    {
                        //flip normal
                        Mesh flippedMesh = Normal.Flip(circle.Mesh);
                        //noise up
                        Vector3[] noiseVert = flippedMesh.vertices;

                        for (int v = 0; v < noiseVert.Length; v++)
                        {
                            noiseVert[v].y -= Mathf.PerlinNoise(noiseVert[v].x * 0.6f, noiseVert[v].z * 0.6f) * 2;
                            noiseVert[v].y += UnityEngine.Random.Range(-1f, 1f);
                        }

                        flippedMesh.vertices = noiseVert;
                        circle.UpdateOuterVertices(flippedMesh.vertices); //Update outerring as well since used in bridgeloop
                        combine[3].mesh = flippedMesh;
                    }

                }

                int circlesLength = Circles.Count;
                //Bridge circles together
                for (int i = 0; i < circlesLength - 1; i++)
                {
                    Circle currentCircle = Circles[i];
                    Circle nextCircle = Circles[(i + 1) % circlesLength];

                    //Bridge Circles together
                    BridgeLoop bridgeLoop = new BridgeLoop(currentCircle.OuterVertices, nextCircle.OuterVertices)
                    {
                        DebugMode = false
                    };

                    BridgeLoops.Add(bridgeLoop);

                    //Add bridged mesh to combine
                    Mesh loop = bridgeLoop.Connect();
                    combine[i + 1].mesh = loop;
                }

                //Combine our chunk parts (Ground, Belt, Root...)
                finalMesh.CombineMeshes(combine, false, false);
                finalMesh.name = "chunk";

                Bounds = finalMesh.bounds;

                return finalMesh;

            }
        }

        private void SetMeshPosition(Mesh mesh, Vector3 position)
        {
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                mesh.vertices[i] = mesh.vertices[i] + position;
            }
        }

    }


}