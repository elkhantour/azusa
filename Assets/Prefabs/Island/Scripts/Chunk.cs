using UnityEngine;
using System.Collections.Generic;
using Utils;

/**
 Chunks are the independant component of an island.
 A chunk basically stacks up many Circles. Chunks turns the 2D basic circles into a SOLO independant island.
 */
namespace Island
{

    public class Chunk
    {

        public enum Part
        {
            Ground,
            Rock,
            COUNT,
        }

        //Base Circle Configuration
        public int Segments = 30;
        public float Radius = 1f;

        //Chunk depth
        public float Depth = 3;
        public List<Vector3> Vertices = new List<Vector3>();
        public List<Circle> Circles { get; private set; } = new List<Circle>();
        public List<BridgeLoop> BridgeLoops { get; private set; } = new List<BridgeLoop>();
        public List<Mesh> PartMeshes = new List<Mesh>();
        public Bounds Bounds;

        public void Generate()
        {
            Depth *= Radius;
            List<Circle> circleConfig = new List<Circle>()
                {
                    //1. Set Ground
                    new Circle(){
                        Name = "ground",
                        Segments = Segments,
                        Radius = Radius,
                        Smooth = true,
                        SmoothThresholdAngle = 160,
                        InnerCircles = new float[] { 1.0f },
			NoiseAmplitude = 30.0f,
                    },
                    //2. Set Belt
                    new Circle()
                    {
                        Name= "belt",
                        Segments = (int) Mathf.Ceil(Segments / 2),
                        Radius = Radius / 1.2f,
                        Position = new Vector3(0, -1 * Depth, 0),
                        Smooth = false
                    },
                    //3. Set Root
                    new Circle()
                    {
                        Name = "root",
                        Segments = (int)Mathf.Ceil(Segments / 3),
                        Radius = Radius / 2.5f,
                        Position = new Vector3(0, -2 * Depth, 0),
                    }
                };


            //Loop through circles to make them spawn 
            foreach (Circle circle in circleConfig)
            {
                circle.Spawn();
                Circles.Add(circle); // cache circle to global array

                if (circle.Name == "ground")
                {
                    PartMeshes.Add(circle.Mesh);
                }

                if (circle.Name == "root")
                {
                    noiseRoot(circle);
                }
            }


            // Create a bridge between the ground, belt and root vertices
            CombineInstance[] combinedRockPart = BridgeRockPart();

            //Combine our chunk parts (Belt + Root...)
            Mesh rockMesh = new Mesh();
            rockMesh.CombineMeshes(combinedRockPart, false, false);
            rockMesh.name = "Island Chunk Rock";
            PartMeshes.Add(rockMesh);
            Bounds = rockMesh.bounds;
        }

        private void noiseRoot(Circle circle)
        {

            //noise up
            Vector3[] noiseVert = circle.Mesh.vertices;

            for (int v = 0; v < noiseVert.Length; v++)
            {
                noiseVert[v].y -= Mathf.PerlinNoise(noiseVert[v].x * 0.6f, noiseVert[v].z * 0.6f) * 2;
                noiseVert[v].y += UnityEngine.Random.Range(-1f, 1f);
            }

            //Update outer ring as well since used in bridgeloop
            circle.UpdateOuterVertices(circle.Mesh.vertices);
            circle.Mesh.vertices = noiseVert;
            circle.Mesh.RecalculateNormals();
        }

        private CombineInstance[] BridgeRockPart()
        {

            CombineInstance[] combine = new CombineInstance[3];

            //Bridge circles together
            for (int i = 0; i < Circles.Count; i++)
            {
                Circle currentCircle = Circles[i];
                Circle nextCircle = Circles[(i + 1) % Circles.Count];

                //Bridge Circles together
                BridgeLoop bridgeLoop = new BridgeLoop(currentCircle.OuterVertices, nextCircle.OuterVertices)
                {
                    DebugMode = false
                };

                BridgeLoops.Add(bridgeLoop);

                //Add bridged mesh to combine
                Mesh loop = bridgeLoop.Connect();
                combine[i].mesh = loop;
            }


            return combine;
        }

        private void SetMeshPosition(Mesh mesh, Vector3 position)
        {
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                mesh.vertices[i] = mesh.vertices[i] + position;
            }
        }

        public Mesh GetPartMesh(Chunk.Part part)
        {
            return PartMeshes[(int)part];
        }

    }


}
