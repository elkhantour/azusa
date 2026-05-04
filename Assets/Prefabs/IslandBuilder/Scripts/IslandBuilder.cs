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
        [Header("Settings")]
        [SerializeField] private GameObject chunkPrefab;
        [SerializeField] private int maxChunks = 10;
        [SerializeField] private float minRadius = 1f;
        [SerializeField] private float maxRadius = 10f;
        [SerializeField] private float scrollSensitivity = 1f;
        [SerializeField] private float radiusPadding = 0f;

        [Header("Controls")]
        [SerializeField] private KeyCode spawnKey = KeyCode.P;
        [SerializeField] private KeyCode outlineKey = KeyCode.L;
        [SerializeField] private KeyCode deleteKey = KeyCode.Delete;

        private List<GameObject> _spawnedChunks = new List<GameObject>();
        private GameObject _currentActiveChunk;
        private GameObject _outline;
        private bool _isPlacingNew = false;
        private bool _isMovingExisting = false;

        void Awake()
        {

            _outline = new GameObject("IslandOutline");
            _outline.AddComponent<MeshFilter>();
            _outline.AddComponent<MeshRenderer>();

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
            if (Input.GetKeyDown(deleteKey) && _isMovingExisting && _currentActiveChunk != null)
            {
                DeleteCurrentChunk();
            }

            if (Input.GetKeyDown(outlineKey) && _currentActiveChunk == null)
            {
                Outline();
            }
        }

        /// <summary>
        /// Calculates a 2D Rect encompassing a list of circles (position + radius).
        /// This is the most accurate way to define the Marching Squares grid area.
        /// </summary>
        public static Rect GetBoundFromCircles(List<RadialMask> circles, float padding = 5f)
        {
            if (circles == null || circles.Count == 0) return new Rect(0, 0, 0, 0);

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

            return new Rect(
                minX - padding,
                minZ - padding,
                (maxX - minX) + (padding * 2),
                (maxZ - minZ) + (padding * 2)
            );
        }


        private void Outline()
        {

            // Map chunks into radial mask for square marching
            List<RadialMask> circles = _spawnedChunks.Select(m => new RadialMask()
            {
                Position = m.transform.position,
                Radius = (m.transform.localScale.x / 2.0f) - radiusPadding,
            }).ToList();

            // Define the area to scan
            Rect bounds = GetBoundFromCircles(circles);

            // Generate
            var generator = new MarchingSquaresOutline(gridSize: 0.5f);
            Mesh outlineMesh = generator.GenerateOutline(circles, bounds);

            List<Vector3> verticesPos = new List<Vector3>();
            outlineMesh.GetVertices(verticesPos);
            Triangulator triangulator = new Triangulator(MeshUtils.ToVector2(verticesPos.ToArray()));

            int[] triangles = Triangulator.Triangulate(verticesPos.ToArray());
            triangles = Triangulator.RemoveOuterRingTriangles(triangles, verticesPos.ToArray(), circles);
            outlineMesh.SetTriangles(triangles, 0);
	    
            _outline.GetComponent<MeshFilter>().mesh = outlineMesh;
        }

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
                _currentActiveChunk.transform.position = new Vector3(hitPoint.x, 0, hitPoint.z);
            }
        }

        private void UpdateChunkRadius()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                // Assuming the "radius" is represented by the localScale of the helper
                Vector3 scale = _currentActiveChunk.transform.localScale;
                float newRadius = Mathf.Clamp(scale.x + (scroll * scrollSensitivity), minRadius, maxRadius);
                _currentActiveChunk.transform.localScale = new Vector3(newRadius, scale.y, newRadius);
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
