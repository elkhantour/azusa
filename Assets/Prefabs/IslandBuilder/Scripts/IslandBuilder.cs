using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Utils;
using Triangulation;

namespace Island
{
    public class IslandBuilder : MonoBehaviour
    {
        [Serializable]
        public class RootProperty
        {
            public int Segments = 30;
            public float Depth = 1.0f;
            public float Shrink = 0.4f;
            public bool Smooth;
        }

        [Header("Builder")]
        [SerializeField] private GameObject chunkPrefab;
        [SerializeField] private int maxChunks = 10;
        [SerializeField] private float minRadius = 1f;
        [SerializeField] private float maxRadius = 10f;
        [SerializeField] private float scrollSensitivity = 1f;
        [SerializeField] private float radiusPadding = 0f;

        [Header("Island")]
        [SerializeField] private RootProperty Belt;
        [SerializeField] private RootProperty Bottom;
        [SerializeField] private Material groundMaterial;
        [SerializeField] private Material rootMaterial;

        [Header("Controls")]
        [SerializeField] private KeyCode spawnKey = KeyCode.P;
        [SerializeField] private KeyCode outlineKey = KeyCode.L;
        [SerializeField] private KeyCode deleteKey = KeyCode.Delete;

        private List<GameObject> _spawnedChunks = new();
        private List<RadialMask> _circlesMask = new();
        private GameObject _currentActiveChunk;
        private GameObject _ground;
        private GameObject _root;
        private bool _isPlacingNew = false;
        private bool _isMovingExisting = false;

        void Awake()
        {
            _ground = CreateGameObject("IslandGround", groundMaterial);
            _root = CreateGameObject("IslandRoot", rootMaterial);
        }

        void Update()
        {
            HandleInput();

            if (_isPlacingNew || _isMovingExisting)
            {
                UpdateChunkPosition();
                UpdateChunkRadius();
                HandlePlacementConfirmation();
            }
            else
            {
                HandleSelection();
            }
        }

        private GameObject CreateGameObject(string name, Material material = null)
        {
            GameObject go = new GameObject(name);
            go.AddComponent<MeshFilter>();
            MeshRenderer rd = go.AddComponent<MeshRenderer>();

            if (material)
            {
                rd.material = material;
            }

            return go;
        }

        private void CameraLock()
        {
            CameraManager.Instance.SetMode(CameraModeType.Locked);
        }

        private void CameraUnlock()
        {
            CameraManager.Instance.SetMode(CameraModeType.Orbit);
        }

        private void HandleInput()
        {
            // Spawn new chunk
            if (Input.GetKeyDown(spawnKey) && !_isPlacingNew && !_isMovingExisting)
            {
                if (_spawnedChunks.Count < maxChunks)
                {
                    SpawnNewChunk();
                }
                else
                {
                    Debug.LogWarning("Max chunk limit reached!");
                }
            }

            // Delete selected chunk
            if (Input.GetKeyDown(deleteKey) && _isMovingExisting &&
                _currentActiveChunk != null)
            {
                DeleteCurrentChunk();
            }

            if (Input.GetKeyDown(outlineKey) && _currentActiveChunk == null)
            {
                GenerateGround();
                GenerateRoot(_ground.GetComponent<MeshFilter>().mesh);
            }
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
            Mesh outlineMesh = generator.GenerateOutline(_circlesMask, bounds);
            outlineMesh = MeshUtils.RemoveDuplicateVertices(outlineMesh);
            MeshUtils.RewindLoop(outlineMesh);

            Triangulate(outlineMesh, _circlesMask);
            outlineMesh.SetUVs(0, Uv.Planar(outlineMesh.vertices));

            _ground.GetComponent<MeshFilter>().mesh = outlineMesh;
        }

        /// <summary>
        /// Generate the island root according to the ground mesh.
        /// </summary>
        /// <remarks>
        /// For the island's root we translate, shrink down the ground mesh and
        /// bridges the different parts together.
        /// </remarks>
        /// <returns>The final root mesh.</returns>
        private void GenerateRoot(Mesh baseMesh)
        {

            // 1. Offset and shrink the root strates (belt, bootom)
            Mesh beltMesh = MeshUtils.Clone(baseMesh);
            // beltMesh = MeshUtils.Decimate(beltMesh, 1.0f);
            Shrink(beltMesh, _circlesMask, 3 * Belt.Shrink);
            MeshUtils.OffsetVertices(beltMesh, new Vector3(0, -Belt.Depth, 0));

            Mesh bottomMesh = MeshUtils.Clone(baseMesh);
            // bottomMesh = MeshUtils.Decimate(beltMesh, 1.3f);
            Shrink(bottomMesh, _circlesMask, 3 * Bottom.Shrink);
            MeshUtils.OffsetVertices(bottomMesh, new Vector3(0, -Bottom.Depth, 0));

            // 2. Bridges the parts together
            CombineInstance[] combine = new CombineInstance[3];

            Mesh baseBeltLoop = MeshBridge.CreateBridgeByProximity(baseMesh.vertices,
                                                                   beltMesh.vertices);
            Mesh beltBottomLoop = MeshBridge.CreateBridgeByProximity(
                beltMesh.vertices, bottomMesh.vertices);

            Normal.Flip(bottomMesh);

            Noise(baseMesh, baseBeltLoop, beltBottomLoop, bottomMesh);

            // 3. Combine mesh in the global root variable
            // Since the blob is generated with marching square, vertex order is not
            // contiguous anymore Which create stray triangles traversing the circles.
            // So require a cleaning pass.
            combine[0].mesh = baseBeltLoop;
            combine[1].mesh = beltBottomLoop;
            combine[2].mesh = bottomMesh;
            Mesh rootMesh = _root.GetComponent<MeshFilter>().mesh;
            rootMesh.Clear();
            rootMesh.CombineMeshes(combine, true, false);
            rootMesh.RecalculateBounds();
            rootMesh.RecalculateNormals();
        }

        // -------------------------------------------------------------------------
        // Helper
        // -------------------------------------------------------------------------

        private void Noise(Mesh baseMesh, Mesh baseBeltMesh, Mesh beltBottomMesh, Mesh bottomMesh)
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
		    RadialMasks = _circlesMask,
		    Items = new List<NoiseItem>
		    {
			new NoiseItem
			{
			    Mesh = baseMesh,
			    Transform = _ground.transform,
			    Range = (0, baseMesh.vertexCount)
			},
			new NoiseItem
			{
			    Mesh = baseBeltMesh,
			    Transform = _root.transform,
			    Range = (0, baseMesh.vertexCount)
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
		    RadialMasks = _circlesMask,
		    Items = new List<NoiseItem>
		    {
			new NoiseItem
			{
			    Mesh = baseBeltMesh,
			    Transform = _root.transform,
			    Range = (baseMesh.vertexCount, baseBeltMesh.vertexCount)
			},
			new NoiseItem
			{
			    Mesh = beltBottomMesh,
			    Transform = _root.transform,
			    Range = (0, baseBeltMesh.vertexCount - baseMesh.vertexCount)
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
		    RadialMasks = _circlesMask,
		    Items = new List<NoiseItem>
		    {
			new NoiseItem
			{
			    Mesh = beltBottomMesh,
			    Transform = _root.transform,
			    Range = (baseBeltMesh.vertexCount - baseMesh.vertexCount, beltBottomMesh.vertexCount)
			},
			new NoiseItem
			{
			    Mesh = bottomMesh,
			    Transform = _root.transform,
			    Range = (0, bottomMesh.vertexCount)
			}
		    }
		}
	    };

	    
            NoiseIsland.Apply(jobs);

        }

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
        /// Dedicated internal method to triangulate the island levels and clean
        /// them according the the spawned circles.
        /// </summary>
        private static void Triangulate(Mesh mesh, List<RadialMask> spawnedCircles)
        {
            int[] triangles = Triangulator.Triangulate(mesh.vertices);
            triangles = Triangulator.RemoveOuterRingTriangles(triangles, mesh.vertices,
                                                              spawnedCircles);
            mesh.SetTriangles(triangles, 0);
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

        // -------------------------------------------------------------------------
        // Input Events Handlers
        // -------------------------------------------------------------------------

        private void SpawnNewChunk()
        {
            _currentActiveChunk = Instantiate(chunkPrefab);
            _isPlacingNew = true;
            _currentActiveChunk.GetComponent<ChunkHelper>().SetActive(true);
            CameraLock();
        }

        private void UpdateChunkPosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            // Create a mathematical plane at y=0
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                _currentActiveChunk.transform.position =
                    new Vector3(hitPoint.x, 0, hitPoint.z);
            }
        }

        private void UpdateChunkRadius()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                // Assuming the "radius" is represented by the localScale of the helper
                Vector3 scale = _currentActiveChunk.transform.localScale;
                float newRadius = Mathf.Clamp(scale.x + (scroll * scrollSensitivity),
                                              minRadius, maxRadius);
                _currentActiveChunk.transform.localScale =
                    new Vector3(newRadius, scale.y, newRadius);
            }
        }

        private void HandlePlacementConfirmation()
        {
            // On click, drop the chunk and return to idle state
            if (Input.GetMouseButtonDown(0))
            {
                if (_isPlacingNew)
                {
                    _spawnedChunks.Add(_currentActiveChunk);
                }

                _isPlacingNew = false;
                _isMovingExisting = false;

                _currentActiveChunk.GetComponent<ChunkHelper>().SetActive(false);
                _currentActiveChunk = null;

                CameraUnlock();
            }
        }

        private void HandleSelection()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    // Check if we hit one of our chunks
                    if (_spawnedChunks.Contains(hit.collider.gameObject))
                    {
                        _currentActiveChunk = hit.collider.gameObject;
                        _currentActiveChunk.GetComponent<ChunkHelper>().SetActive(true);
                        _isMovingExisting = true;
                    }
                }
            }
        }

        private void DeleteCurrentChunk()
        {
            _spawnedChunks.Remove(_currentActiveChunk);
            Destroy(_currentActiveChunk);
            _currentActiveChunk = null;
            _isMovingExisting = false;
            _isPlacingNew = false;
        }
    }

}
