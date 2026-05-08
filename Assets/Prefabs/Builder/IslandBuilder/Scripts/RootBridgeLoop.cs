using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;
using System.Linq;

namespace Island
{
    [Serializable]
    public class RootBridgeLoop
    {

        [Serializable]
        public class RootProperty
        {
            public int Segments = 30;
            public float Depth = 1.0f;
            public float Shrink = 0.4f;
            public bool Smooth;
        }


        [SerializeField] private RootProperty Belt;
        [SerializeField] private RootProperty Bottom;


        // Inner mesh parts cache, used during the noise process
        public class Meshes
        {
            public Mesh Top;    // Root Top Part
            public Mesh Bottom; // Root Bottom Part
            public Mesh Cap;    // Root Cap Part
        }

        public Meshes Parts = new();


        /// <summary>
        /// Generate the island root according to the ground mesh.
        /// </summary>
        /// <remarks>
        /// For the island's root we translate, shrink down the ground mesh and
        /// bridges the different parts together.
        /// </remarks>
        public Mesh Generate(Mesh baseMesh, List<RadialMask> circlesMask)
        {
            // 1. Offset and shrink the root strates (belt, bootom)
            Mesh topLoop = MeshUtils.Clone(baseMesh);
            // topLoop = MeshUtils.Decimate(topLoop, 1.0f);
            Shrink(topLoop, circlesMask, 3 * Belt.Shrink);
            MeshUtils.OffsetVertices(topLoop, new Vector3(0, -Belt.Depth, 0));

            Parts.Cap = MeshUtils.Clone(baseMesh);
            // _rootParts.Cap = MeshUtils.Decimate(topLoop, 1.3f);
            Shrink(Parts.Cap, circlesMask, 3 * Bottom.Shrink);
            MeshUtils.OffsetVertices(Parts.Cap, new Vector3(0, -Bottom.Depth, 0));

            // 2. Bridges the parts together
            Parts.Top = MeshBridge.CreateBridgeByProximity(baseMesh.vertices, topLoop.vertices);
            Parts.Bottom = MeshBridge.CreateBridgeByProximity(topLoop.vertices, Parts.Cap.vertices);

            Normal.Flip(Parts.Cap);

            return GetCombinedMesh();
        }

        private Mesh GetCombinedMesh()
        {
            CombineInstance[] combine = new CombineInstance[3];
            combine[0].mesh = Parts.Top;
            combine[1].mesh = Parts.Bottom;
            combine[2].mesh = Parts.Cap;

            Mesh combinedMesh = new Mesh();
            combinedMesh.Clear();
            combinedMesh.CombineMeshes(combine, true, false);
            combinedMesh.RecalculateBounds();
            combinedMesh.RecalculateNormals();

            return combinedMesh;
        }


        public void Noise(Mesh groundMesh, Mesh rootMesh, List<RadialMask> circlesMask, Transform transform)
        {

            List<NoiseJob> jobs = new List<NoiseJob>
        {
		// Ground -> Belt Begin
		new NoiseJob
        {
            Name = "Ground to Belt",
            Noise = new NoiseSettings
            {
            Mode = NoiseMode.XZRadial,
            Amplitude = 0.6f
            },
            RadialMasks = circlesMask,
            Items = new List<NoiseItem>
            {
            new NoiseItem
            {
                Mesh = groundMesh,
                Transform = transform,
                Range = (0, groundMesh.vertexCount)
            },
            new NoiseItem
            {
                Mesh = Parts.Top,
                Transform = transform,
                Range = (0, groundMesh.vertexCount)
            }
            }
        },
		
		// Belt End -> Bottom Begin
		new NoiseJob
        {
            Name = "Belt End to Bottom Begin",
            Noise = new NoiseSettings
            {
            Mode = NoiseMode.XZRadialPlusY,
            Amplitude = 0.5f,
            YAmplitude = 0.4f
            },
            RadialMasks = circlesMask,
            Items = new List<NoiseItem>
            {
            new NoiseItem
            {
                Mesh = Parts.Top,
                Transform = transform,
                Range = (groundMesh.vertexCount, Parts.Top.vertexCount)
            },
            new NoiseItem
            {
                Mesh = Parts.Bottom,
                Transform = transform,
                Range = (0, Parts.Top.vertexCount - groundMesh.vertexCount)
            }
            }
        },
		
		// Bottom end -> Cap
		new NoiseJob
        {
            Name = "Bottom End to Cap",
            Noise = new NoiseSettings
            {
            Mode = NoiseMode.PureY,
            YAmplitude = 0.8f
            },
            RadialMasks = circlesMask,
            Items = new List<NoiseItem>
            {
            new NoiseItem
            {
                Mesh = Parts.Bottom,
                Transform = transform,
                Range = (Parts.Top.vertexCount - groundMesh.vertexCount, Parts.Bottom.vertexCount)
            },
            new NoiseItem
            {
                Mesh = Parts.Cap,
                Transform = transform,
                Range = (0, Parts.Cap.vertexCount)
            }
            }
        }
        };


            NoiseIsland.Apply(jobs);

        }

        /// <summary>
        /// Shrink the mesh vertices based on a distance-filed.
        /// </summary>
        /// <remarks>
        /// For eact mesh vertices we check their distance on each radial masks. The
        /// closer, the harder the pull.
        /// </remarks>
        private static void Shrink(Mesh mesh, List<RadialMask> srcMasks,
                                   float strength)
        {

            Debug.Log(mesh);
            Debug.Log(srcMasks);

            // Subdivide our masks with average points for more accurate distance field
            List<RadialMask> masks = AddMiddleAnchor(srcMasks);
            List<Vector3> vertices = mesh.vertices.ToList();
            List<Vector3> result = new(vertices.Count);

            foreach (var v in vertices)
            {
                Vector3 total = Vector3.zero;
                float totalWeight = 0f;

                foreach (var m in masks)
                {
                    Vector3 toCenter =
                        new Vector3(m.Position.x - v.x, 0f, m.Position.y - v.z);

                    float dist = toCenter.magnitude;
                    float radius = Mathf.Max(m.Radius, 0.0001f);

                    // smooth decay (never fully zero)
                    float t = dist / radius;
                    float weight = 1f / (1f + t * t);

                    if (dist > 0.0001f)
                        total += toCenter.normalized * weight;

                    totalWeight += weight;
                }

                Vector3 final = v;

                if (totalWeight > 0f)
                {
                    Vector3 avg = total / totalWeight;
                    final += avg * strength;
                }

                result.Add(final);
            }

            mesh.vertices = result.ToArray();
            mesh.RecalculateBounds();
        }



        /// <summary>
        /// Generate additional intermediate masks
        /// placed at the midpoint between every pair of existing masks.
        /// </summary>
        /// <remarks>
        /// The function does not modify the original masks but returns a new list
        /// containing both the original masks and the generated midpoint anchors.
        /// The midpoint mask position is computed as the average of two mask
        /// positions, and its radius is the average of both radii.
        ///
        /// Note: This operation has O(n²) complexity and may significantly increase
        /// the number of masks for large input lists.
        ///
        /// Usecase: We mostly use this function to make the distance-field shrinking
        /// process more accurate and keep the vertices inside.
        /// </remarks>
        public static List<RadialMask> AddMiddleAnchor(List<RadialMask> masks)
        {
            var result = new List<RadialMask>(masks);

            int count = masks.Count;

            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    RadialMask a = masks[i];
                    RadialMask b = masks[j];

                    Vector2 midPos = (a.Position + b.Position) * 0.5f;

                    float midRadius = (a.Radius + b.Radius) * 0.5f;

                    result.Add(new RadialMask { Position = midPos, Radius = midRadius });
                }
            }

            return result;
        }


    }

}
