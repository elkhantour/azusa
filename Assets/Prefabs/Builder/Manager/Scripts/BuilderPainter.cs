using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Island
{
    namespace Builder
    {

        public enum PainterMode
        {
            Island,
            NomadTown,
            Temple,
        }

        public class BuilderPainter : MonoBehaviour
        {

            [Header("Builder")]
            [SerializeField] protected GameObject chunkPrefab;
            [SerializeField] protected int maxChunks = 10;
            [SerializeField] protected float minRadius = 1f;
            [SerializeField] protected float maxRadius = 10f;
            [SerializeField] protected float scrollSensitivity = 1f;
            [SerializeField] protected float radiusPadding = 0f;

            [Header("Controls")]
            [SerializeField] private KeyCode spawnKey = KeyCode.P;
            [SerializeField] private KeyCode deleteKey = KeyCode.Delete;


            protected List<GameObject> _spawnedChunks = new();
            protected List<RadialMask> _circlesMask = new();
            protected GameObject _currentActiveChunk;
            protected bool _isPlacingNew = false;
            protected bool _isMovingExisting = false;

            protected virtual void HandlePlacementConfirmation() { }

	    public void Enable() {
		
	    }
	    
            public void Disable() {
		
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


            protected void CameraLock()
            {
                CameraManager.Instance.SetMode(CameraModeType.Locked);
            }

            protected void CameraUnlock()
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
}
