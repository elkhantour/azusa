using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Utils;
using Triangulation;

namespace Island
{
    namespace Builder
    {
        public class IslandPainter : BuilderPainter
        {

            public enum RootCreateMethod
            {
                BridgeLoop,
                MeshNet
            };

            [Header("Root")]
            [SerializeField] private RootCreateMethod _rootCreateMethod = RootCreateMethod.BridgeLoop;
            [SerializeField] private RootBridgeLoop _rootBridgeLoop = new();
            [SerializeField] private RootNetMesh _rootNetMesh = new();

            void Awake()
            {
	        GenerateRandom();
            }


            private GameObject CreateGameObject(string name, Material material = null, GameObject parent = null)
            {
                GameObject go = new GameObject(name);
                go.AddComponent<MeshFilter>();
                MeshRenderer rd = go.AddComponent<MeshRenderer>();

                if (material)
                {
                    rd.material = material;
                }

                if (parent)
                {
                    go.transform.SetParent(parent.transform);
                }

                return go;
            }


            // -------------------------------------------------------------------------
            // Generators
            // -------------------------------------------------------------------------

            /// <summary>
            /// Convert the spawned circles into mesh through marchisquare and Delaunay
            /// triangulation.
            /// </summary>
            /// <returns>The final mesh.</returns>
            private void GenerateGround()
            {

                // Map chunks into radial mask for square marching
                _circlesMask =
                    _spawnedChunks
                        .Select(m => new RadialMask()
                        {
                            Position = m.transform.position,
                            Radius = (m.transform.localScale.x / 2.0f) - radiusPadding,
                        })
                        .ToList();

                // Define the area to scan
                Rect bounds = GetBoundFromCircles(_circlesMask);

                // Generate
                var generator = new MarchingSquaresOutline(gridSize: 0.5f);
                Mesh outlined = generator.GenerateOutline(_circlesMask, bounds);
                outlined = MeshUtils.RemoveDuplicateVertices(outlined);

                MeshUtils.RewindLoop(outlined);

                int[] triangles = Triangulator.Triangulate(outlined.vertices);
                triangles = Triangulator.RemoveOuterRingTriangles(triangles, outlined.vertices, _circlesMask);
                outlined.SetTriangles(triangles, 0);
                outlined.SetUVs(0, Uv.Planar(outlined.vertices));

                _island.SetGroundMesh(outlined);
            }

            private void GenerateRoot()
            {

                Mesh rootMesh = new();

                switch (_rootCreateMethod)
                {
                    case RootCreateMethod.BridgeLoop:
                        rootMesh = _rootBridgeLoop.Generate(_island.GetGroundMesh(), _circlesMask);
                        _rootBridgeLoop.Noise(_island.GetGroundMesh(), rootMesh, _circlesMask, _island.transform);
                        break;

                    case RootCreateMethod.MeshNet:
                        rootMesh = _rootNetMesh.Generate(_island.GetGroundMesh(), _circlesMask);
                        _rootNetMesh.Noise(_island.GetGroundMesh(), rootMesh, _circlesMask, _island.transform);
                        break;
                }

                _island.SetRootMesh(rootMesh);
                _island.BakeTexture();
            }


            // -------------------------------------------------------------------------
            // Helper
            // -------------------------------------------------------------------------

            /// <summary>
            /// Calculates a 2D Rect encompassing a list of circles (position + radius).
            /// This is the most accurate way to define the Marching Squares grid area.
            /// </summary>
            public static Rect GetBoundFromCircles(List<RadialMask> circles,
                                                   float padding = 5f)
            {
                if (circles == null || circles.Count == 0)
                    return new Rect(0, 0, 0, 0);

                float minX = float.MaxValue;
                float minZ = float.MaxValue;
                float maxX = float.MinValue;
                float maxZ = float.MinValue;

                foreach (var circle in circles)
                {
                    // We expand the bounds by the radius in all four directions
                    minX = Mathf.Min(minX, circle.Position.x - circle.Radius);
                    maxX = Mathf.Max(maxX, circle.Position.x + circle.Radius);
                    minZ = Mathf.Min(minZ, circle.Position.z - circle.Radius);
                    maxZ = Mathf.Max(maxZ, circle.Position.z + circle.Radius);
                }

                return new Rect(minX - padding, minZ - padding,
                                (maxX - minX) + (padding * 2),
                                (maxZ - minZ) + (padding * 2));
            }

            /// <summary>
            /// Removes any triangle whose centroid falls outside all RadialMasks.
            /// Works directly on an existing mesh — call this after BridgeConnect.
            /// </summary>
            private static void RemoveStrayTriangles(Mesh mesh, List<RadialMask> masks,
                                                     float radiusBias = 0f)
            {
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;

                List<int> kept = new List<int>(triangles.Length);

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 centroid = (vertices[triangles[i]] + vertices[triangles[i + 1]] +
                                        vertices[triangles[i + 2]]) /
                                       3f;

                    if (!IsInsideAnyMask(centroid, masks, radiusBias))
                    {
                        kept.Add(triangles[i]);
                        kept.Add(triangles[i + 1]);
                        kept.Add(triangles[i + 2]);
                    }
                }

                mesh.triangles = kept.ToArray();
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
            }

            private static bool IsInsideAnyMask(Vector3 point, List<RadialMask> masks,
                                                float bias)
            {
                foreach (var mask in masks)
                {
                    Vector3 delta = point - mask.Position;
                    delta.y = 0f;
                    float r = mask.Radius + bias;
                    if (delta.sqrMagnitude <= r * r)
                        return true;
                }
                return false;
            }


            // -------------------------------------------------------------------------
            // Input Events Handlers
            // -------------------------------------------------------------------------

            protected override void OnPlacementConfirmation()
            {
                        GenerateGround();
                        GenerateRoot();
                        _island.GenerateFlora();
            }


            // -------------------------------------------------------------------------
            // Debug
            // -------------------------------------------------------------------------
            [ContextMenu("Generate Random")]
            private void GenerateRandom()
            {

                // Clean up any existing chunks first
                foreach (var chunk in _spawnedChunks)
                    Destroy(chunk);
                _spawnedChunks.Clear();

                int count = UnityEngine.Random.Range(2, maxChunks + 1);

                for (int i = 0; i < count; i++)
                {
                    GameObject chunk = Instantiate(chunkPrefab);

                    // Random radius for this chunk
                    float radius = UnityEngine.Random.Range(minRadius, maxRadius);
                    chunk.transform.localScale = new Vector3(radius * 2f, chunk.transform.localScale.y, radius * 2f);

                    // First chunk anchors at origin, subsequent ones attach to a random existing chunk
                    if (i == 0)
                    {
                        chunk.transform.position = Vector3.zero;
                    }
                    else
                    {
                        // Pick a random already-placed chunk as anchor
                        GameObject anchor = _spawnedChunks[UnityEngine.Random.Range(0, _spawnedChunks.Count)];
                        float anchorRadius = anchor.transform.localScale.x / 2f;

                        // Place at a random angle, at a distance guaranteed to overlap
                        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                        float maxOverlapDist = anchorRadius + radius; // touching = sum of radii, so stay under this
                        float distance = UnityEngine.Random.Range(maxOverlapDist * 0.2f, maxOverlapDist * 0.85f);

                        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
                        chunk.transform.position = anchor.transform.position + offset;
                    }

                    chunk.GetComponent<ChunkHelper>().SetActive(false);
                    _spawnedChunks.Add(chunk);
                }

                UpdateChunksVisibility();
		OnPlacementConfirmation();
            }
        }

    }
}
