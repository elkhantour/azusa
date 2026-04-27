using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        [Header("Controls")]
        [SerializeField] private KeyCode spawnKey = KeyCode.P;
        [SerializeField] private KeyCode deleteKey = KeyCode.Delete;

        private List<GameObject> spawnedChunks = new List<GameObject>();
        private GameObject currentActiveChunk;
        private bool isPlacingNew = false;
        private bool isMovingExisting = false;

        void Update()
        {
            HandleInput();

            if (isPlacingNew || isMovingExisting)
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

        private void HandleInput()
        {
            // Spawn new chunk
            if (Input.GetKeyDown(spawnKey) && !isPlacingNew && !isMovingExisting)
            {
                if (spawnedChunks.Count < maxChunks)
                {
                    SpawnNewChunk();
                }
                else
                {
                    Debug.LogWarning("Max chunk limit reached!");
                }
            }

            // Delete selected chunk
            if (Input.GetKeyDown(deleteKey) && isMovingExisting && currentActiveChunk != null)
            {
                DeleteCurrentChunk();
            }
        }

        private void SpawnNewChunk()
        {
            currentActiveChunk = Instantiate(chunkPrefab);
            isPlacingNew = true;
            currentActiveChunk.GetComponent<ChunkHelper>().SetActive(true);
        }

        private void UpdateChunkPosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            // Create a mathematical plane at y=0
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                currentActiveChunk.transform.position = new Vector3(hitPoint.x, 0, hitPoint.z);
            }
        }

        private void UpdateChunkRadius()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                // Assuming the "radius" is represented by the localScale of the helper
                Vector3 scale = currentActiveChunk.transform.localScale;
                float newRadius = Mathf.Clamp(scale.x + (scroll * scrollSensitivity), minRadius, maxRadius);
                currentActiveChunk.transform.localScale = new Vector3(newRadius, scale.y, newRadius);
            }
        }

        private void HandlePlacementConfirmation()
        {
            // On click, drop the chunk and return to idle state
            if (Input.GetMouseButtonDown(0))
            {
                if (isPlacingNew)
                {
                    spawnedChunks.Add(currentActiveChunk);
                }

                isPlacingNew = false;
                isMovingExisting = false;

                currentActiveChunk.GetComponent<ChunkHelper>().SetActive(false);
                currentActiveChunk = null;
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
                    if (spawnedChunks.Contains(hit.collider.gameObject))
                    {
                        currentActiveChunk = hit.collider.gameObject;
                        currentActiveChunk.GetComponent<ChunkHelper>().SetActive(true);
                        isMovingExisting = true;
                    }
                }
            }
        }

        private void DeleteCurrentChunk()
        {
            spawnedChunks.Remove(currentActiveChunk);
            Destroy(currentActiveChunk);
            currentActiveChunk = null;
            isMovingExisting = false;
            isPlacingNew = false;
        }
    }

}
